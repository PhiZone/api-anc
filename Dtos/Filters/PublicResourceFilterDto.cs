using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class PublicResourceFilterDto<T> : FilterDto<T> where T : PublicResource
{
    public List<Guid>? RangeId { get; set; }
    
    public string? ContainsTitle { get; set; }
    
    public string? EqualsTitle { get; set; }
    
    public string? ContainsIllustrator { get; set; }
    
    public string? EqualsIllustrator { get; set; }
    
    public string? ContainsDescription { get; set; }
    
    public string? EqualsDescription { get; set; }
    
    public List<Accessibility>? RangeAccessibility { get; set; }
    
    public bool? IsHidden { get; set; }
    
    public bool? IsLocked { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
    
    public DateTimeOffset? EarliestDateUpdated { get; set; }
    
    public DateTimeOffset? LatestDateUpdated { get; set; }
    
    public int? MinLikeCount { get; set; }
    
    public int? MaxLikeCount { get; set; }
    
    public List<int>? RangeLikeCount { get; set; }
}