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
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Services;

public class MailService : IMailService
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IConnectionMultiplexer _redis;
    private readonly MailSettings _settings;
    private readonly ITemplateService _templateService;

    public MailService(IOptions<MailSettings> options, ITemplateService templateService,
        IRabbitMqService rabbitMqService, IConnectionMultiplexer redis)
    {
        _settings = options.Value;
        _templateService = templateService;
        _rabbitMqService = rabbitMqService;
        _redis = redis;
    }

    public async Task<MailTaskDto?> GenerateEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action)
    {
        if (user.Email == null || user.UserName == null) throw new ArgumentNullException(nameof(user));

        string code;
        var random = new Random();
        var db = _redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString()[1..];
        } while (await db.KeyExistsAsync($"EMAIL:{mode}:{code}"));
        
        if (!await db.StringSetAsync($"EMAIL:{mode}:{code}", user.Email, TimeSpan.FromSeconds(305))) return null;

        var template = _templateService.GetEmailTemplate(mode, user.Language);

        return new MailTaskDto
        {
            User = user,
            EmailSubject = template["Subject"],
            EmailBody = _templateService.ReplacePlaceholders(template["Body"],
                new Dictionary<string, string> { { "UserName", user.UserName }, { "Code", code } }),
            SucceedingAction = action
        };
    }

    public async Task<string> PublishEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action)
    {
        var mailDto = await GenerateEmailAsync(user, mode, action);
        if (mailDto == null) return ResponseCodes.RedisError;

        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(mailDto));

        try
        {
            using var channel = _rabbitMqService.GetConnection().CreateModel();
            channel.BasicPublish("", "email", false, null, body);
        }
        catch (Exception)
        {
            return ResponseCodes.MailError;
        }

        var db = _redis.GetDatabase();
        await db.StringSetAsync($"COOLDOWN:{mode}:{user.Email}", DateTimeOffset.UtcNow.AddMinutes(5).ToString(),
            TimeSpan.FromMinutes(5));
        return string.Empty;
    }

    public async Task<string> SendMailAsync(MailTaskDto mailTaskDto)
    {
        using var emailMessage = new MimeMessage();
        var emailFrom = new MailboxAddress(_settings.SenderName, _settings.SenderAddress);
        emailMessage.From.Add(emailFrom);
        var emailTo = new MailboxAddress(mailTaskDto.User.UserName, mailTaskDto.User.Email);
        emailMessage.To.Add(emailTo);

        // emailMessage.Cc.Add(new MailboxAddress("Cc Receiver", "cc@example.com"));
        // emailMessage.Bcc.Add(new MailboxAddress("Bcc Receiver", "bcc@example.com"));

        emailMessage.Subject = mailTaskDto.EmailSubject;

        var emailBodyBuilder = new BodyBuilder { TextBody = mailTaskDto.EmailBody };

        emailMessage.Body = emailBodyBuilder.ToMessageBody();

        try
        {
            using var mailClient = new SmtpClient();
            await mailClient.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.SslOnConnect);
            await mailClient.AuthenticateAsync(_settings.UserName, _settings.Password);
            await mailClient.SendAsync(emailMessage);
            await mailClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return ex.Message;
        }

        return string.Empty;
    }
}