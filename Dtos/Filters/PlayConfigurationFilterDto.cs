using System.Runtime.Serialization;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class PlayConfigurationFilterDto : FilterDto<PlayConfiguration>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }

    public int? MinPerfectJudgment { get; set; }

    public int? MaxPerfectJudgment { get; set; }

    public List<int>? RangePerfectJudgment { get; set; }

    public int? MinGoodJudgment { get; set; }

    public int? MaxGoodJudgment { get; set; }

    public List<int>? RangeGoodJudgment { get; set; }

    public List<ChartMirroringMode>? RangeChartMirroring { get; set; }

    public double? MaxNoteSize { get; set; }

    public double? MinNoteSize { get; set; }

    public double? MinBackgroundLuminance { get; set; }

    public double? MaxBackgroundLuminance { get; set; }

    public double? MinBackgroundBlur { get; set; }

    public double? MaxBackgroundBlur { get; set; }

    public bool? HasSimultaneousNoteHint { get; set; }

    public bool? HasFcApIndicator { get; set; }

    public int? MinChartOffset { get; set; }

    public int? MaxChartOffset { get; set; }

    public List<int>? RangeChartOffset { get; set; }

    public double? MinHitSoundVolume { get; set; }

    public double? MaxHitSoundVolume { get; set; }

    public double? MinMusicVolume { get; set; }

    public double? MaxMusicVolume { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    [IgnoreDataMember] public List<int>? RangeOwnerId { get; set; }
}