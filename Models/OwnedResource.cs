namespace PhiZoneApi.Models;

public abstract class OwnedResource : Resource
{
    public int OwnerId { get; set; }

    public User Owner { get; set; } = null!;
}