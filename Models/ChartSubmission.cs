using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ChartSubmission : Submission
{
    public string? Title { get; set; }

    public ChartLevel LevelType { get; set; }

    public string Level { get; set; } = null!;

    public double Difficulty { get; set; }

    public ChartFormat Format { get; set; }

    public string? File { get; set; }

    public string? FileChecksum { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public bool IsRanked { get; set; }

    public int NoteCount { get; set; }

    public Guid? SongId { get; set; }

    public Song? Song { get; set; }

    public Guid? SongSubmissionId { get; set; }

    public SongSubmission? SongSubmission { get; set; }

    public RequestStatus VolunteerStatus { get; set; }

    public RequestStatus AdmissionStatus { get; set; }

    [JsonIgnore] public List<VolunteerVote> VolunteerVotes { get; } = [];
}