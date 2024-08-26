using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class UserRelationDto
{
    public int FollowerId { get; set; }

    public int FolloweeId { get; set; }

    public UserRelationType Type { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}