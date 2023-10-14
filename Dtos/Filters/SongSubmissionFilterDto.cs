using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class SongSubmissionFilterDto : FilterDto<SongSubmission>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsTitle { get; set; }

    public string? EqualsTitle { get; set; }

    public List<EditionType>? RangeEditionType { get; set; }

    public string? ContainsEdition { get; set; }

    public string? EqualsEdition { get; set; }

    public string? ContainsAuthorName { get; set; }

    public string? EqualsAuthorName { get; set; }

    public string? ContainsIllustrator { get; set; }

    public string? EqualsIllustrator { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public List<Accessibility>? RangeAccessibility { get; set; }

    public List<RequestStatus>? RangeStatus { get; set; }

    public List<Guid>? /* supposed to be nullable */ RangeRepresentationId { get; set; }

    public string? ContainsLyrics { get; set; }

    public string? EqualsLyrics { get; set; }

    public int? MinBpm { get; set; }

    public int? MaxBpm { get; set; }

    public List<int>? RangeBpm { get; set; }

    public int? MinMinBpm { get; set; }

    public int? MaxMinBpm { get; set; }

    public List<int>? RangeMinBpm { get; set; }

    public int? MinMaxBpm { get; set; }

    public int? MaxMaxBpm { get; set; }

    public List<int>? RangeMaxBpm { get; set; }

    public int? MinOffset { get; set; }

    public int? MaxOffset { get; set; }

    public List<int>? RangeOffset { get; set; }

    public TimeSpan? MinDuration { get; set; }

    public TimeSpan? MaxDuration { get; set; }

    public TimeSpan? MinPreviewStart { get; set; }

    public TimeSpan? MaxPreviewStart { get; set; }

    public TimeSpan? MinPreviewEnd { get; set; }

    public TimeSpan? MaxPreviewEnd { get; set; }

    public int? MinReviewerId { get; set; }

    public int? MaxReviewerId { get; set; }

    public List<int>? /* supposed to be nullable */ RangeReviewerId { get; set; }

    public string? ContainsMessage { get; set; }

    public string? EqualsMessage { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public DateTimeOffset? EarliestDateUpdated { get; set; }

    public DateTimeOffset? LatestDateUpdated { get; set; }
}