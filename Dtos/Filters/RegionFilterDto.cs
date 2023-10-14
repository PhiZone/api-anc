using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class RegionFilterDto : FilterDto<Region>
{
    public int? MinId { get; set; }

    public int? MaxId { get; set; }

    public List<int>? RangeId { get; set; }

    public string? ContainsCode { get; set; }

    public string? EqualsCode { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }
}