using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Responses;

public class EventTaskDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public EventTaskType Type { get; set; }

    public string? Code { get; set; }

    public bool IsHidden { get; set; }

    public string Description { get; set; } = null!;

    public Guid DivisionId { get; set; }

    public DateTimeOffset? DateExecuted { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset DateUpdated { get; set; }
}