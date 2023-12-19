namespace PhiZoneApi.Models;

public abstract class Resource
{
    public Guid Id { get; set; }

    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }
}