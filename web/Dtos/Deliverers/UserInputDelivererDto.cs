namespace PhiZoneApi.Dtos.Deliverers;

public class UserInputDelivererDto
{
    public string Content { get; set; } = null!;

    public bool IsImage { get; set; }

    public string MemberName { get; set; } = null!;

    public string? ResourceId { get; set; }

    public string ActionName { get; set; } = null!;

    public string ControllerName { get; set; } = null!;

    public string RequestPath { get; set; } = null!;

    public string RequestMethod { get; set; } = null!;

    public int? UserId { get; set; }

    public string? UserName { get; set; }

    public string? UserAvatar { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}