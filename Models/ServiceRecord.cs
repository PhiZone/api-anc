namespace PhiZoneApi.Models;

public class ServiceRecord : Resource
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Result { get; set; }

    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public ServiceScript Service { get; set; } = null!;
}