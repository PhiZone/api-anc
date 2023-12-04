using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class CollectionFilterDto : PublicResourceFilterDto<Collection>
{
    public string? ContainsSubtitle { get; set; }

    public string? EqualsSubtitle { get; set; }
}