using PhiZoneApi.Dtos;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    bool SendMail(MailDto mailDto);
}