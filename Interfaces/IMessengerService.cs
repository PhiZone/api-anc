using PhiZoneApi.Dtos.Deliverers;

namespace PhiZoneApi.Interfaces;

public interface IMessengerService
{
    Task<HttpResponseMessage> SendMail(MailTaskDto dto);

    Task<HttpResponseMessage> SendUserInput(UserInputDelivererDto dto);

    Task<HttpResponseMessage> Proxy(HttpRequestMessage message);
}