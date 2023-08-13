using PhiZoneApi.Dtos.Requests;

namespace PhiZoneApi.Interfaces;

public interface ITapTapService
{
    Task<HttpResponseMessage?> Login(TapTapRequestDto dto);
}