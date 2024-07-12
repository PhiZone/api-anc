namespace PhiZoneApi.Dtos.Requests;

public class EventHostInviteRequestDto
{

    public bool IsAdmin { get; set; }

    public bool IsUnveiled { get; set; }

    public List<uint> Permissions { get; set; } = [];

    public string? Position { get; set; }
}