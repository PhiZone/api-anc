using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventTask
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public EventTaskType Type { get; set; }

    public string? Code { get; set; }

    public bool IsHidden { get; set; }

    public string Description { get; set; } = null!;

    public Guid DivisionId { get; set; }

    public EventDivision Division { get; set; } = null!;

    public DateTimeOffset? DateExecuted { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    public string GetDisplay()
    {
        return Name;
    }
}