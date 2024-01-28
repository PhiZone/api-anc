namespace PhiZoneApi.Dtos.Responses;

public class UserRelationDto
{
    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}