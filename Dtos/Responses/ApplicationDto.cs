using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class ApplicationDto
{
    public Guid Id { get; set; }

    public string Avatar { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public string Homepage { get; set; } = null!;

    public string? ApiEndpoint { get; set; }

    public ApplicationType Type { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}