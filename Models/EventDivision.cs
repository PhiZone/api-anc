using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventDivision : PublicResource
{
    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    public EventDivisionType Type { get; set; }

    public EventDivisionStatus Status { get; set; }

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public int? MinTeamCount { get; set; }

    public int? MaxTeamCount { get; set; }

    public int? MinParticipantPerTeamCount { get; set; }

    public int? MaxParticipantPerTeamCount { get; set; }

    public int? MinSubmissionCount { get; set; }

    public int? MaxSubmissionCount { get; set; }

    public Guid EventId { get; set; }

    public Event Event { get; set; } = null!;

    public DateTimeOffset DateUnveiled { get; set; }

    public DateTimeOffset DateStarted { get; set; }

    public DateTimeOffset DateEnded { get; set; }

    [JsonIgnore] public List<User> Administrators { get; set; } = [];

    public override string GetDisplay()
    {
        return Subtitle != null ? $"{Title} - {Subtitle}" : Title;
    }
}