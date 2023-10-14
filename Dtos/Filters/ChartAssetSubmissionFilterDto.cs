using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ChartAssetSubmissionFilterDto : FilterDto<ChartAssetSubmission>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeChartSubmissionId { get; set; }

    public List<ChartAssetType>? RangeType { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public DateTimeOffset? EarliestDateUpdated { get; set; }

    public DateTimeOffset? LatestDateUpdated { get; set; }
}