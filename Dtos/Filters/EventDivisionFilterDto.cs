using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class EventDivisionFilterDto : PublicResourceFilterDto<EventDivision>
{
    public string? ContainsSubtitle { get; set; }

    public string? EqualsSubtitle { get; set; }

    public List<EventDivisionType>? RangeType { get; set; }

    public List<EventDivisionStatus>? RangeStatus { get; set; }

    public int? MinMinTeamCount { get; set; }

    public int? MaxMinTeamCount { get; set; }

    public int? MinMaxTeamCount { get; set; }

    public int? MaxMaxTeamCount { get; set; }

    public int? MinMinParticipantPerTeamCount { get; set; }

    public int? MaxMinParticipantPerTeamCount { get; set; }

    public int? MinMaxParticipantPerTeamCount { get; set; }

    public int? MaxMaxParticipantPerTeamCount { get; set; }

    public int? MinMinSubmissionCount { get; set; }

    public int? MaxMinSubmissionCount { get; set; }

    public int? MinMaxSubmissionCount { get; set; }

    public int? MaxMaxSubmissionCount { get; set; }
    
    public bool? HasAnonymization { get; set; }

    public List<Guid>? RangeEventId { get; set; }

    public DateTimeOffset? EarliestDateUnveiled { get; set; }

    public DateTimeOffset? LatestDateUnveiled { get; set; }

    public DateTimeOffset? EarliestDateStarted { get; set; }

    public DateTimeOffset? LatestDateStarted { get; set; }

    public DateTimeOffset? EarliestDateEnded { get; set; }

    public DateTimeOffset? LatestDateEnded { get; set; }
}