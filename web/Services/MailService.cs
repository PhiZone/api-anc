using System.Text;
using Newtonsoft.Json;
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
    IMessengerService messengerService,
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
            var response = await messengerService.SendMail(mailTaskDto);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(LogEvents.MailFailure,
                    "Failed to send an email to {Email} for {User}:\n{Content}",
                    mailTaskDto.EmailAddress, mailTaskDto.UserName,
                    await response.Content.ReadAsStringAsync());
                return response.StatusCode.ToString();
            }
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