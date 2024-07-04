namespace PhiZoneApi.Dtos.Responses;

public class ServiceRecordDto
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Result { get; set; }

    public int OwnerId { get; set; }

    public Guid ServiceId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}