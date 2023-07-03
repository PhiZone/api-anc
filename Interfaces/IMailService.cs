using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    Task<MailDto?> GenerateEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action);

    Task<string> PublishEmailAsync(User user, EmailRequestMode mode, SucceedingAction? action);

    Task<string> SendMailAsync(MailDto mailDto);
}