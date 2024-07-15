using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class EventDivisionDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    public EventDivisionType Type { get; set; }

    public EventDivisionStatus Status { get; set; }

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public Guid? TagId { get; set; }

    public int? MinTeamCount { get; set; }

    public int? MaxTeamCount { get; set; }

    public int? MinParticipantPerTeamCount { get; set; }

    public int? MaxParticipantPerTeamCount { get; set; }

    public int? MinSubmissionCount { get; set; }

    public int? MaxSubmissionCount { get; set; }

    public bool Anonymization { get; set; }

    public string? SuggestedEntrySearch { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUnveiled { get; set; }

    public DateTimeOffset DateStarted { get; set; }

    public DateTimeOffset DateEnded { get; set; }

    public int TeamCount { get; set; }

    public int EntryCount { get; set; }

    public DateTimeOffset? DateLiked { get; set; }

    public EventTeamDto? Team { get; set; }
}