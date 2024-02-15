namespace PhiZoneApi.Dtos.Responses;

public class ApplicationServiceRecordDto
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public Guid ServiceId { get; set; }

    public string? Description { get; set; }

    public string? Result { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}