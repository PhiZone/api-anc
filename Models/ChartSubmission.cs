using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class ChartSubmission
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

    public Guid SongId { get; set; }

    public Song Song { get; set; } = null!;

    public IEnumerable<User> Authors { get; } = new List<User>();
}