using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ChartSubmissionFilterDto : FilterDto<ChartSubmission>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsTitle { get; set; }

    public string? EqualsTitle { get; set; }

    public string? ContainsIllustrator { get; set; }

    public string? EqualsIllustrator { get; set; }

    public List<ChartLevel>? RangeLevelType { get; set; }

    public string? ContainsLevel { get; set; } = null!;

    public string? EqualsLevel { get; set; } = null!;

    public double? MinDifficulty { get; set; }

    public double? MaxDifficulty { get; set; }

    public List<double>? RangeDifficulty { get; set; }

    public List<ChartFormat>? RangeFormat { get; set; }

    public string? ContainsAuthorName { get; set; } = null!;

    public string? EqualsAuthorName { get; set; } = null!;

    public bool? IsRanked { get; set; }

    public int? MinNoteCount { get; set; }

    public int? MaxNoteCount { get; set; }

    public List<int>? RangeNoteCount { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public List<Accessibility>? RangeAccessibility { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}