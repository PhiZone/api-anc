using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class EventResourceRequestDto
{
    public Guid DivisionId { get; set; }

    public EventResourceType Type { get; set; }

    public string? Label { get; set; }

    public string? Description { get; set; }

    public bool? IsAnonymous { get; set; }

    public double? Score { get; set; }

    public List<string?> Reserved { get; set; } = [];

    public Guid? TeamId { get; set; }
}