using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventTeam : Resource
{
    public string Name { get; set; } = null!;

    public string Icon { get; set; } = null!;

    public ParticipationStatus Status { get; set; }

    public int? ClaimedParticipantCount { get; set; }

    public int? ClaimedSubmissionCount { get; set; }

    public double? Score { get; set; }

    [JsonIgnore] public IEnumerable<User> Participants { get; set; } = new List<User>();

    [JsonIgnore] public IEnumerable<Participation> Participations { get; set; } = new List<Participation>();

    public string GetDisplay()
    {
        return Name;
    }
}