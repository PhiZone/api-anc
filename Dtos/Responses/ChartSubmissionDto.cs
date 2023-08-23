using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ChartSubmissionDto
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public ChartLevel LevelType { get; set; }

    public string Level { get; set; } = null!;

    public double Difficulty { get; set; }

    public ChartFormat Format { get; set; }

    public string? File { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public RequestStatus Status { get; set; }

    public RequestStatus VolunteerStatus { get; set; }

    public RequestStatus AdmissionStatus { get; set; }

    public Guid? RepresentationId { get; set; }

    public bool IsRanked { get; set; }

    public int NoteCount { get; set; }

    public Guid? SongId { get; set; }

    public Guid? SongSubmissionId { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
    
    public DateTimeOffset? DateVoted { get; set; }
}