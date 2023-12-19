using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventDivision : PublicResource
{
    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }
    
    public EventDivisionType Type { get; set; }

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }
    
    public Guid EventId { get; set; }
    
    public Event Event { get; set; } = null!;

    public override string GetDisplay()
    {
        return Subtitle != null ? $"{Title} - {Subtitle}" : Title;
    }
}