using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    Task<MailTaskDto?> GenerateEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action);

    Task<string> PublishEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action);

    Task<string> SendMailAsync(MailTaskDto mailTaskDto);
}