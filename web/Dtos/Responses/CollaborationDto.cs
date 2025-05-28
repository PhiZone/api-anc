using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class CollaborationDto
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public int InviterId { get; set; }

    public int InviteeId { get; set; }

    public string? Position { get; set; }

    public RequestStatus Status { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}