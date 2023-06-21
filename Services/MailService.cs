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
    private readonly MailSettings _mailSettings;

    public MailService(IOptions<MailSettings> mailSettingsOptions)
    {
        _mailSettings = mailSettingsOptions.Value;
    }

    public bool SendMail(MailDto mailDto)
    {
        try
        {
            using var emailMessage = new MimeMessage();
            var emailFrom = new MailboxAddress(_mailSettings.SenderName, _mailSettings.SenderAddress);
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
            mailClient.Connect(_mailSettings.Server, _mailSettings.Port, SecureSocketOptions.SslOnConnect);
            mailClient.Authenticate(_mailSettings.UserName, _mailSettings.Password);
            mailClient.Send(emailMessage);
            mailClient.Disconnect(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }
}