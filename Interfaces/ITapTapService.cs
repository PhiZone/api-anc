using PhiZoneApi.Dtos.Requests;

namespace PhiZoneApi.Interfaces;

public interface ITapTapService
{
    Task<HttpResponseMessage> Login(TapLoginRequestDto dto);

    // Task<(string, string)> GetTokens(User user);
}