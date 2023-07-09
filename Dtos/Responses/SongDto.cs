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
    
    public int Bpm { get; set; }
    
    public int MinBpm { get; set; }
    
    public int MaxBpm { get; set; }
    
    public int Offset { get; set; }
    
    public bool IsOriginal { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    public TimeSpan PreviewStart { get; set; }
    
    public TimeSpan PreviewEnd { get; set; }
    
    public IEnumerable<int> AuthorsId { get; set; } = null!;

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
    
    public DateTimeOffset DateUpdated { get; set; }
    
    public int CommentCount { get; set; }
    
    public int LikeCount { get; set; }
    
    public DateTimeOffset? DateLiked { get; set; }
}