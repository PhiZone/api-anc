using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace PhiZoneApi.Services;

public class MailService(
    ITemplateService templateService,
    IRabbitMqService rabbitMqService,
    IConnectionMultiplexer redis,
    IOptions<MailSettings> mailSettings,
    IHostEnvironment env,
    ILogger<MailService> logger) : IMailService
{
    private readonly string _queue = env.IsProduction() ? "email" : "email-dev";

    public async Task<MailTaskDto?> GenerateEmailAsync(string email, string userName, string language,
        EmailRequestMode mode, bool useHtml = false)
    {
        string code;
        var random = new Random();
        var db = redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString()[1..];
        } while (await db.KeyExistsAsync($"phizone:email:{mode}:{code}"));

        if (!await db.StringSetAsync($"phizone:email:{mode}:{code}", email, TimeSpan.FromSeconds(305))) return null;

        var template = templateService.GetEmailTemplate(mode, language)!;

        return new MailTaskDto
        {
            EmailAddress = email,
            UserName = userName,
            EmailSubject = template.Subject,
            EmailBody = templateService.ReplacePlaceholders(template.Body,
                new Dictionary<string, string> { { "UserName", userName }, { "Code", code } }),
            UseHtml = false
        };
    }

    public async Task<string> PublishEmailAsync(string email, string userName, string language, EmailRequestMode mode)
    {
        var mailDto = await GenerateEmailAsync(email, userName, language, mode);
        if (mailDto == null) return ResponseCodes.RedisError;

        try
        {
            await PublishEmailAsync(mailDto);
        }
        catch (Exception)
        {
            return ResponseCodes.MailError;
        }

        var db = redis.GetDatabase();
        await db.StringSetAsync($"phizone:cooldown:{mode}:{email}", DateTimeOffset.UtcNow.AddMinutes(5).ToString(),
            TimeSpan.FromMinutes(5));
        return string.Empty;
    }

    public async Task<string> PublishEmailAsync(MailTaskDto mailDto)
    {
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mailDto));

        try
        {
            await using var channel = await rabbitMqService.GetConnection().CreateChannelAsync();
            await channel.BasicPublishAsync("", _queue, false, new BasicProperties(), body);
        }
        catch (Exception)
        {
            return ResponseCodes.MailError;
        }

        return string.Empty;
    }

    public async Task<string> SendMailAsync(MailTaskDto mailTaskDto)
    {
        try
        {
            var settings = mailSettings.Value;
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.SenderName, settings.SenderAddress));
            message.To.Add(MailboxAddress.Parse(mailTaskDto.EmailAddress));
            message.Subject = $"[PhiZone] {mailTaskDto.EmailSubject}";
            message.Body = mailTaskDto.UseHtml
                ? new BodyBuilder { HtmlBody = mailTaskDto.EmailBody }.ToMessageBody()
                : new TextPart("plain") { Text = mailTaskDto.EmailBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Server, settings.Port, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(settings.UserName, settings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(LogEvents.MailFailure, ex, "Failed to send an email to {Email} for {User}",
                mailTaskDto.EmailAddress, mailTaskDto.UserName);
            return ex.Message;
        }

        return string.Empty;
    }
}
