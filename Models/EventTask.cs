namespace PhiZoneApi.Models;

public class Schedule
{
    public Guid Id { get; set; }
    
    public string Code { get; set; } = null!;

    public DateTimeOffset DateExecuted { get; set; }
    
    public DateTimeOffset DateCreated { get; set; }
    
    public DateTimeOffset DateUpdated { get; set; }
    
    public int OwnerId { get; set; }
    
    public User Owner { get; set; } = null!;
}