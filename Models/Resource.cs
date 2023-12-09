using System.Runtime.Serialization;

namespace PhiZoneApi.Models;

public abstract class Resource
{
    public Guid Id { get; set; }

    public int OwnerId { get; set; }

    [IgnoreDataMember]
    public User Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }
}