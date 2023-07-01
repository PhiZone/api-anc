namespace PhiZoneApi.Dtos;

public class UserRelationDto
{
    public UserDto Follower { get; set; } = null!;

    public UserDto Followee { get; set; } = null!;

    public DateTimeOffset Time { get; set; }
}