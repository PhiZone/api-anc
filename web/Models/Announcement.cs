using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Announcement : LikeableResource
{
    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTimeOffset DateUpdated { get; set; }

    public Guid? ResourceId { get; set; }

    public PublicResource? Resource { get; set; }

    public PublicResourceType ResourceType { get; set; }

    public override string GetDisplay()
    {
        return $"{Title}";
    }
}