using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos;
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

    public async Task<MailDto?> GenerateEmailAsync(User user, EmailRequestMode mode)
    {
        if (user.Email == null || user.UserName == null) throw new ArgumentNullException(nameof(user));

        string code;
        var random = new Random();
        var db = _redis.GetDatabase();
        do
        {
            code = random.Next(1000000, 2000000).ToString()[1..];
        } while (await db.KeyExistsAsync($"EMAIL{mode}:{code}"));

        if (!await db.StringSetAsync($"EMAIL{mode}:{code}", user.Id, TimeSpan.FromMinutes(5))) return null;

        var template = _templateService.GetEmailTemplate(mode, user.Language);

        return new MailDto
        {
            RecipientAddress = user.Email,
            RecipientName = user.UserName,
            EmailSubject = template["Subject"],
            EmailBody = _templateService.ReplacePlaceholders(template["Body"],
                new Dictionary<string, string> { { "UserName", user.UserName }, { "Code", code } })
        };
    }

    public async Task<string> PublishEmailAsync(User user, EmailRequestMode mode)
    {
        var mailDto = await GenerateEmailAsync(user, mode);
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
        await db.StringSetAsync($"COOLDOWN{mode}:{user.Email}", DateTimeOffset.UtcNow.AddMinutes(5).ToString(),
            TimeSpan.FromMinutes(5));
        return string.Empty;
    }

    public async Task SendMailAsync(MailDto mailDto)
    {
        using var emailMessage = new MimeMessage();
        var emailFrom = new MailboxAddress(_settings.SenderName, _settings.SenderAddress);
        emailMessage.From.Add(emailFrom);
        var emailTo = new MailboxAddress(mailDto.RecipientName, mailDto.RecipientAddress);
        emailMessage.To.Add(emailTo);

        // emailMessage.Cc.Add(new MailboxAddress("Cc Receiver", "cc@example.com"));
        // emailMessage.Bcc.Add(new MailboxAddress("Bcc Receiver", "bcc@example.com"));

        emailMessage.Subject = mailDto.EmailSubject;

        var emailBodyBuilder = new BodyBuilder { TextBody = mailDto.EmailBody };

        emailMessage.Body = emailBodyBuilder.ToMessageBody();

        using var mailClient = new SmtpClient();
        await mailClient.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.SslOnConnect);
        await mailClient.AuthenticateAsync(_settings.UserName, _settings.Password);
        await mailClient.SendAsync(emailMessage);
        await mailClient.DisconnectAsync(true);
    }
}