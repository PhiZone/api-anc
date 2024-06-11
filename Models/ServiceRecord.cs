namespace PhiZoneApi.Models;

public class ServiceRecord
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public PublicResource Resource { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public ServiceScript ServiceScript { get; set; } = null!;

    public string? Description { get; set; }

    public string? Result { get; set; }

    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }
}