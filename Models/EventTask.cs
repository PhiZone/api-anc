using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventTask
{
    public Guid Id { get; set; }
    
    public TaskType Type { get; set; }
    
    public string? Code { get; set; }
    
    public bool IsHidden { get; set; }

    public DateTimeOffset? DateExecuted { get; set; }
    
    public DateTimeOffset DateCreated { get; set; }
    
    public DateTimeOffset DateUpdated { get; set; }
    
    public Guid DivisionId { get; set; }
    
    public EventDivision Division { get; set; } = null!;
}