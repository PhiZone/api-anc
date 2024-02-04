using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ChartDto
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

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public bool IsRanked { get; set; }

    public int NoteCount { get; set; }

    public double Score { get; set; }

    public double Rating { get; set; }

    public double RatingOnArrangement { get; set; }

    public double RatingOnGameplay { get; set; }

    public double RatingOnVisualEffects { get; set; }

    public double RatingOnCreativity { get; set; }

    public double RatingOnConcord { get; set; }

    public double RatingOnImpression { get; set; }

    public List<TagDto> Tags { get; set; } = null!;

    public Guid SongId { get; set; }

    public SongDto Song { get; set; } = null!;

    public int PlayCount { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}