using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Responses;

public class NotificationDto
{
    public Guid Id { get; set; }

    public NotificationType Type { get; set; }

    public string Content { get; set; } = null!;

    public User? Operator { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset? DateRead { get; set; }
}