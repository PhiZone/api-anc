using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class CollaborationFilterDto : FilterDto<Collaboration>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeSubmissionId { get; set; }

    public int? MinInviterId { get; set; }

    public int? MaxInviterId { get; set; }

    public List<int>? RangeInviterId { get; set; }

    public int? MinInviteeId { get; set; }

    public int? MaxInviteeId { get; set; }

    public List<int>? RangeInviteeId { get; set; }

    public string? ContainsPosition { get; set; }

    public string? EqualsPosition { get; set; }

    public List<string>? /* supposed to be nullable */ RangePosition { get; set; }

    public List<RequestStatus>? RangeStatus { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}