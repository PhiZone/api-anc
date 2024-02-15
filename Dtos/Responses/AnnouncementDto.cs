using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class AnnouncementDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int LikeCount { get; set; }

    public Guid? ResourceId { get; set; }

    public PublicResourceType ResourceType { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}