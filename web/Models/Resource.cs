namespace PhiZoneApi.Models;

public abstract class Resource
{
    public Guid Id { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}