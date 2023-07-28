using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Collaboration
{
    public Guid Id { get; set; }

    public Guid ChartId { get; set; }

    public ChartSubmission Chart { get; set; } = null!;

    public int InviterId { get; set; }

    public User Inviter { get; set; } = null!;

    public int InviteeId { get; set; }

    public User Invitee { get; set; } = null!;

    public RequestStatus Status { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}