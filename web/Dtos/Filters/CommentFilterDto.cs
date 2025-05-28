using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class CommentFilterDto : FilterDto<Comment>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeResourceId { get; set; }

    public string? ContainsContent { get; set; }

    public string? EqualsContent { get; set; }

    public string? ContainsLanguage { get; set; }

    public string? EqualsLanguage { get; set; }

    public List<string>? RangeLanguage { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public int? MinLikeCount { get; set; }

    public int? MaxLikeCount { get; set; }

    public List<int>? RangeLikeCount { get; set; }
}