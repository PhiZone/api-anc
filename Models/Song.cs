using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Song : PublicResource
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

    public int Bpm { get; set; }

    public int MinBpm { get; set; }

    public int MaxBpm { get; set; }

    public int Offset { get; set; }

    public bool IsOriginal { get; set; }

    public TimeSpan? Duration { get; set; }

    public TimeSpan PreviewStart { get; set; }

    public TimeSpan PreviewEnd { get; set; }

    public IEnumerable<Chapter> Chapters { get; } = new List<Chapter>();
    
    public IEnumerable<Admission> ChapterAdmitters { get; } = new List<Admission>();

    public IEnumerable<User> Authors { get; } = new List<User>();
}