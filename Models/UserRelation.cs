namespace PhiZoneApi.Models;

public class UserRelation
{
    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    public User Follower { get; set; } = null!;

    public User Followee { get; set; } = null!;

    public DateTimeOffset Time { get; set; }
}