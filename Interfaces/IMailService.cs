using PhiZoneApi.Dtos;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    Task SendMailAsync(MailDto mailDto);
}