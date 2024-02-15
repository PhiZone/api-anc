using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public abstract class PublicResource : LikeableResource
{
    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}