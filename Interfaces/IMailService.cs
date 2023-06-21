using PhiZoneApi.Dtos;

namespace PhiZoneApi.Interfaces;

public interface IMailService
{
    void SendMail(MailDto mailDto);
}