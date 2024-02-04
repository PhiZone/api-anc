using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class TagFilterDto : FilterDto<Tag>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }

    public string? ContainsNormalizedName { get; set; }

    public string? EqualsNormalizedName { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}