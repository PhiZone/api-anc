using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class UserRelationFilterDto : FilterDto<UserRelation>
{
    public int? MinFollowerId { get; set; }

    public int? MaxFollowerId { get; set; }

    public List<int>? RangeFollowerId { get; set; }

    public int? MinFolloweeId { get; set; }

    public int? MaxFolloweeId { get; set; }

    public List<int>? RangeFolloweeId { get; set; }

    public DateTimeOffset? EarliestTime { get; set; }

    public DateTimeOffset? LatestTime { get; set; }
}