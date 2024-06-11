using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class EventFilterDto : PublicResourceFilterDto<Event>
{
    public string? ContainsSubtitle { get; set; }

    public string? EqualsSubtitle { get; set; }

    public DateTimeOffset? EarliestDateUnveiled { get; set; }

    public DateTimeOffset? LatestDateUnveiled { get; set; }
}