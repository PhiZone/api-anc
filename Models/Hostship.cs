namespace PhiZoneApi.Models;

public class Hostship
{
    public Guid EventId { get; set; }

    public Event Event { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public string? Position { get; set; }
    
    public bool IsUnveiled { get; set; }
    
    public bool IsAdmin { get; set; }
    
    public List<uint> Permissions { get; set; } = [];
}