using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public abstract class PublicResource
{
    public Guid Id { get; set; }

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public int LikeCount { get; set; }
}