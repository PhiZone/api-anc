using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PhiZoneApi.Configurations;
using PhiZoneApi.Dtos;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class MailService : IMailService
{
    private readonly MailSettings _settings;

    public MailService(IOptions<MailSettings> options)
    {
        _settings = options.Value;
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

        var emailBodyBuilder = new BodyBuilder
        {
            TextBody = mailDto.EmailBody
        };

        emailMessage.Body = emailBodyBuilder.ToMessageBody();

        using var mailClient = new SmtpClient();
        await mailClient.ConnectAsync(_settings.Server, _settings.Port, SecureSocketOptions.SslOnConnect);
        await mailClient.AuthenticateAsync(_settings.UserName, _settings.Password);
        await mailClient.SendAsync(emailMessage);
        await mailClient.DisconnectAsync(true);
    }
}