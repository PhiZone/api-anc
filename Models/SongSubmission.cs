using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class SongSubmission : Submission
{
    public string Title { get; set; } = null!;

    public EditionType EditionType { get; set; }

    public string? Edition { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? File { get; set; }

    public string? FileChecksum { get; set; }

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Lyrics { get; set; }

    public double Bpm { get; set; }

    public double MinBpm { get; set; }

    public double MaxBpm { get; set; }

    public int Offset { get; set; }

    public string? License { get; set; }

    public string? OriginalityProof { get; set; }

    public TimeSpan? Duration { get; set; }

    public TimeSpan PreviewStart { get; set; }

    public TimeSpan PreviewEnd { get; set; }

    public int? ReviewerId { get; set; }

    public User? Reviewer { get; set; }

    public string? Message { get; set; }

    public string GetDisplay()
    {
        return Title;
    }
}