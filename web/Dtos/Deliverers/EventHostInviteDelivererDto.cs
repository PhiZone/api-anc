namespace PhiZoneApi.Dtos.Deliverers;

public class EventHostInviteDelivererDto
{
    public Guid EventId { get; set; }

    public int InviterId { get; set; }

    public string Code { get; set; } = null!;

    public DateTimeOffset DateExpired { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsUnveiled { get; set; }

    public string? Position { get; set; }

    public List<uint> Permissions { get; set; } = [];
}