namespace PhiZoneApi.Models;

public class UserRelation
{
    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    public required User Follower { get; set; }

    public required User Followee { get; set; }

    public DateTimeOffset Time { get; set; }
}