using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ChapterFilterDto : PublicResourceFilterDto<Chapter>
{
    public string? ContainsSubtitle { get; set; }

    public string? EqualsSubtitle { get; set; }
}