using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class EventDto
{
    public Guid Id { get; set; }
    
    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public Accessibility Accessibility { get; set; }

    public bool IsHidden { get; set; }

    public bool IsLocked { get; set; }
    
    public List<DivisionDto> Divisions { get; set; } = null!;

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public DateTimeOffset DateUnveiled { get; set; }

    public DateTimeOffset? DateLiked { get; set; }

    public List<UserDto> Administrators { get; set; } = [];
}

public class DivisionDto
{
    public string Title { get; set; } = null!;
    
    public string? Subtitle { get; set; }

    public EventDivisionType Type { get; set; }

    public EventDivisionStatus Status { get; set; }
}