using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class UserRelation
{
    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    public User Follower { get; set; } = null!;

    public User Followee { get; set; } = null!;

    public UserRelationType Type { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}