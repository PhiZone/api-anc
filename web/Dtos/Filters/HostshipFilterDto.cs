using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class HostshipFilterDto : FilterDto<Hostship>
{
    public List<Guid>? RangeEventId { get; set; }

    public List<int>? RangeUserId { get; set; }

    public bool? IsAdmin { get; set; }

    public bool? IsUnveiled { get; set; }

    public string? ContainsPosition { get; set; }

    public string? EqualsPosition { get; set; }
}