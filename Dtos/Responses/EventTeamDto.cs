using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class EventTeamDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Icon { get; set; }

    public string? Description { get; set; }

    public ParticipationStatus Status { get; set; }

    public int? ClaimedParticipantCount { get; set; }

    public int? ClaimedSubmissionCount { get; set; }

    public double? Score { get; set; }

    public int? Position { get; set; }

    public Guid DivisionId { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }

    public List<PositionalUserDto> Participants { get; set; } = null!;
}