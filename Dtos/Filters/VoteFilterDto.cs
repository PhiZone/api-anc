using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class VoteFilterDto : FilterDto<Vote>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeChartId { get; set; }

    public int? MinArrangement { get; set; }

    public int? MaxArrangement { get; set; }

    public List<int>? RangeArrangement { get; set; }

    public int? MinGameplay { get; set; }

    public int? MaxGameplay { get; set; }

    public List<int>? RangeGameplay { get; set; }

    public int? MinVisualEffects { get; set; }

    public int? MaxVisualEffects { get; set; }

    public List<int>? RangeVisualEffects { get; set; }

    public int? MinCreativity { get; set; }

    public int? MaxCreativity { get; set; }

    public List<int>? RangeCreativity { get; set; }

    public int? MinConcord { get; set; }

    public int? MaxConcord { get; set; }

    public List<int>? RangeConcord { get; set; }

    public int? MinImpression { get; set; }

    public int? MaxImpression { get; set; }

    public List<int>? RangeImpression { get; set; }

    public int? MinTotal { get; set; }

    public int? MaxTotal { get; set; }

    public List<int>? RangeTotal { get; set; }

    public double? MinMultiplier { get; set; }

    public double? MaxMultiplier { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}