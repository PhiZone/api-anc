using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Collaboration : Resource
{
    public Guid SubmissionId { get; set; }

    public Submission Submission { get; set; } = null!;

    public int InviterId { get; set; }

    public User Inviter { get; set; } = null!;

    public int InviteeId { get; set; }

    public User Invitee { get; set; } = null!;

    public string? Position { get; set; }

    public RequestStatus Status { get; set; }
}