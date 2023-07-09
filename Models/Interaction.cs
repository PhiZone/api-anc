namespace PhiZoneApi.Models;

public abstract class Interaction
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }
    
    public PublicResource Resource { get; set; } = null!;

    public int UserId { get; set; }
    
    public User User { get; set; } = null!;
    
    public DateTimeOffset DateCreated { get; set; }
}