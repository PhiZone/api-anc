namespace PhiZoneApi.Dtos.Deliverers;

public class EventTeamInviteDelivererDto
{
    public Guid TeamId { get; set; }

    public int InviterId { get; set; }

    public string Code { get; set; } = null!;

    public DateTimeOffset DateExpired { get; set; }
}