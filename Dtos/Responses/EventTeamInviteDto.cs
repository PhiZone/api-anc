namespace PhiZoneApi.Dtos.Responses;

public class EventTeamInviteDto
{
    public EventTeamDto Team { get; set; } = null!;

    public UserDto Inviter { get; set; } = null!;

    public DateTimeOffset DateExpired { get; set; }
}