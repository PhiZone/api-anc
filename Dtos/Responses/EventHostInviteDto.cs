namespace PhiZoneApi.Dtos.Responses;

public class EventHostInviteDto
{
    public EventDto Event { get; set; } = null!;

    public UserDto Inviter { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTimeOffset DateExpired { get; set; }
    
    public bool IsAdmin { get; set; }
    
    public bool IsUnveiled { get; set; }
    
    public string? Position { get; set; }
    
    public List<uint> Permissions { get; set; } = [];
}