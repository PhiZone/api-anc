using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ServiceResponseDto
{
    public ServiceResponseType Type { get; set; }

    public string? RedirectUri { get; set; }

    public string? Message { get; set; }
}