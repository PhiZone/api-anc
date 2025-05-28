using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Notification : OwnedResource
{
    public NotificationType Type { get; set; }

    public string Content { get; set; } = null!;

    public int? OperatorId { get; set; }

    public User? Operator { get; set; }

    public DateTimeOffset? DateRead { get; set; }
}