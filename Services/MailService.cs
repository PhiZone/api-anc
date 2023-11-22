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
using StackExchange.Redis;

namespace PhiZoneApi.Services;

public class MailService(IOptions<MailSettings> options, ITemplateService templateService,
        IRabbitMqService rabbitMqService, IConnectionMultiplexer redis, ILogger<MailService> logger)
    : IMailService
{
    private readonly MailSettings _settings = options.Value;

    public async Task<MailTaskDto?> GenerateEmailAsync(string email, string userName, string language,
        EmailRequestMode mode)
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
                new Dictionary<string, string> { { "UserName", userName }, { "Code", code } })
        };
    }

    public async Task<string> PublishEmailAsync(string email, string userName, string language, EmailRequestMode mode)
    {
        var mailDto = await GenerateEmailAsync(email, userName, language, mode);
        if (mailDto == null) return ResponseCodes.RedisError;

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mailDto));

        try
        {
            using var channel = rabbitMqService.GetConnection().CreateModel();
            channel.BasicPublish("", "email", false, null, body);
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

    public async Task<string> SendMailAsync(MailTaskDto mailTaskDto)
    {
        try
        {
            using var emailMessage = new MimeMessage();
            var emailFrom = new MailboxAddress(_settings.SenderName, _settings.SenderAddress);
            emailMessage.From.Add(emailFrom);
            var emailTo = new MailboxAddress(mailTaskDto.UserName, mailTaskDto.EmailAddress);
            emailMessage.To.Add(emailTo);

            emailMessage.Subject = mailTaskDto.EmailSubject;

            var emailBodyBuilder = new BodyBuilder { TextBody = mailTaskDto.EmailBody };

            emailMessage.Body = emailBodyBuilder.ToMessageBody();

            using var mailClient = new SmtpClient();
            await mailClient.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.SslOnConnect);
            await mailClient.AuthenticateAsync(_settings.UserName, _settings.Password);
            await mailClient.SendAsync(emailMessage);
            await mailClient.DisconnectAsync(true);
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