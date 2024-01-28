using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ChapterDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Subtitle { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public int LikeCount { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}