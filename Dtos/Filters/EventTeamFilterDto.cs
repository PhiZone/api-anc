using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class EventTeamFilterDto : FilterDto<EventTeam>
{
    public List<Guid>? RangeId { get; set; }

    public string? ContainsName { get; set; }

    public string? EqualsName { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public List<ParticipationStatus>? RangeStatus { get; set; }

    public int? MinClaimedParticipantCount { get; set; }

    public int? MaxClaimedParticipantCount { get; set; }

    public int? MinClaimedSubmissionCount { get; set; }

    public int? MaxClaimedSubmissionCount { get; set; }

    public double? MinScore { get; set; }

    public double? MaxScore { get; set; }

    public List<Guid>? RangeDivisionId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}