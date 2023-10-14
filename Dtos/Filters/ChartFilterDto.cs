using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ChartFilterDto : PublicResourceFilterDto<Chart>
{
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

    public double? MinScore { get; set; }

    public double? MaxScore { get; set; }

    public List<double>? RangeScore { get; set; }

    public double? MinRating { get; set; }

    public double? MaxRating { get; set; }

    public List<double>? RangeRating { get; set; }

    public double? MinRatingOnArrangement { get; set; }

    public double? MaxRatingOnArrangement { get; set; }

    public List<double>? RangeRatingOnArrangement { get; set; }

    public double? MinRatingOnFeel { get; set; }

    public double? MaxRatingOnFeel { get; set; }

    public List<double>? RangeRatingOnFeel { get; set; }

    public double? MinRatingOnVisualEffects { get; set; }

    public double? MaxRatingOnVisualEffects { get; set; }

    public List<double>? RangeRatingOnVisualEffects { get; set; }

    public double? MinRatingOnCreativity { get; set; }

    public double? MaxRatingOnCreativity { get; set; }

    public List<double>? RangeRatingOnCreativity { get; set; }

    public double? MinRatingOnConcord { get; set; }

    public double? MaxRatingOnConcord { get; set; }

    public List<double>? RangeRatingOnConcord { get; set; }

    public double? MinRatingOnImpression { get; set; }

    public double? MaxRatingOnImpression { get; set; }

    public List<double>? RangeRatingOnImpression { get; set; }

    public List<Guid>? RangeSongId { get; set; }

    public int? MinPlayCount { get; set; }

    public int? MaxPlayCount { get; set; }

    public List<int>? /* supposed to be nullable */ RangePlayCount { get; set; }
}