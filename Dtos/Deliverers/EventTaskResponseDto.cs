using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Deliverers;

public class EventTaskResponseDto
{
    public ResponseStatus Status { get; set; }

    public string Code { get; set; } = null!;

    public string? Message { get; set; }
}