using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ApplicationServiceFilterDto : FilterDto<ApplicationService>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }

    public List<ServiceTargetType>? RangeTargetType { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public List<Guid>? RangeApplicationId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}