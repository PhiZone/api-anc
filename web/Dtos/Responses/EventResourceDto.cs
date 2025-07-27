using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class EventResourceDto
{
    public Guid DivisionId { get; set; }

    public Guid ResourceId { get; set; }

    public EventResourceType Type { get; set; }

    public string? Label { get; set; }

    public string? Description { get; set; }

    public bool? IsAnonymous { get; set; }

    public double? Score { get; set; }

    public Guid? TeamId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}