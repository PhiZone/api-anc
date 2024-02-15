using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class NotificationFilterDto : FilterDto<Notification>
{
    public List<Guid>? RangeId { get; set; }

    public List<NotificationType>? RangeType { get; set; }

    public string? ContainsContent { get; set; } = null!;

    public string? EqualsContent { get; set; } = null!;

    public int? MinOperatorId { get; set; }

    public int? MaxOperatorId { get; set; }

    public List<int>? /* supposed to be nullable */ RangeOperatorId { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public DateTimeOffset? EarliestDateRead { get; set; }

    public DateTimeOffset? LatestDateRead { get; set; }
}