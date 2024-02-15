using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class SongDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public EditionType EditionType { get; set; }

    public string Edition { get; set; } = null!;

    public string AuthorName { get; set; } = null!;

    public string File { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public string? Lyrics { get; set; }

    public double Bpm { get; set; }

    public double MinBpm { get; set; }

    public double MaxBpm { get; set; }

    public int Offset { get; set; }

    public string? License { get; set; }

    public bool IsOriginal { get; set; }

    public TimeSpan Duration { get; set; }

    public TimeSpan PreviewStart { get; set; }

    public TimeSpan PreviewEnd { get; set; }

    public List<ChartLevelDto> ChartLevels { get; set; } = new(Enum.GetValues<ChartLevel>().Length);

    public List<TagDto> Tags { get; set; } = null!;

    public int PlayCount { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}

public class ChartLevelDto
{
    public ChartLevel LevelType { get; set; }

    public int Count { get; set; }
}