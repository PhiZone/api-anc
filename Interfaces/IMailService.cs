using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    Task<MailTaskDto?> GenerateEmailAsync(string email, string userName, string language, EmailRequestMode mode);

    Task<string> PublishEmailAsync(string email, string userName, string language, EmailRequestMode mode);

    Task<string> PublishEmailAsync(MailTaskDto mailDto);

    Task<string> SendMailAsync(MailTaskDto mailTaskDto);
}