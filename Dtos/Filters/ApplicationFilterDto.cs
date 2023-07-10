using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ApplicationFilterDto : FilterDto<Application>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }

    public string? ContainsIllustrator { get; set; }

    public string? EqualsIllustrator { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public string? ContainsHomepage { get; set; }

    public string? EqualsHomepage { get; set; }

    public string? ContainsApiEndpoint { get; set; }

    public string? EqualsApiEndpoint { get; set; }

    public List<ApplicationType>? RangeType { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public int? MinLikeCount { get; set; }

    public int? MaxLikeCount { get; set; }

    public List<int>? RangeLikeCount { get; set; }
}