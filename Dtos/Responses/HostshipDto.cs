namespace PhiZoneApi.Dtos.Responses;

public class HostshipDto
{
    public Guid EventId { get; set; }

    public int UserId { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsUnveiled { get; set; }

    public string? Position { get; set; }
}