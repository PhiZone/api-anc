using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class EventResourceFilterDto : FilterDto<EventResource>
{
    public List<Guid>? RangeDivisionId { get; set; }

    public List<Guid>? RangeResourceId { get; set; }

    public List<EventResourceType>? RangeType { get; set; }

    public string? ContainsLabel { get; set; }

    public string? EqualsLabel { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public bool? IsAnonymous { get; set; }

    public List<Guid>? RangeTeamId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}