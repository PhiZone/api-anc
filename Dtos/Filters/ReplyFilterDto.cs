using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ReplyFilterDto : FilterDto<Reply>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeCommentId { get; set; }

    public string? ContainsContent { get; set; } = null!;

    public string? EqualsContent { get; set; } = null!;

    public string? ContainsLanguage { get; set; } = null!;

    public string? EqualsLanguage { get; set; } = null!;

    public List<string>? RangeLanguage { get; set; } = null!;

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public int? MinLikeCount { get; set; }

    public int? MaxLikeCount { get; set; }

    public List<int>? RangeLikeCount { get; set; }
}