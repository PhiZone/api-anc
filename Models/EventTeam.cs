using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class EventTeam : LikeableResource, IComparable<EventTeam>
{
    public string Name { get; set; } = null!;

    public string? Icon { get; set; }

    public string? Description { get; set; }

    public ParticipationStatus Status { get; set; }

    public int? ClaimedParticipantCount { get; set; }

    public int? ClaimedSubmissionCount { get; set; }

    public bool IsUnveiled { get; set; }

    public double? Score { get; set; }

    [JsonIgnore] public List<string?> Preserved { get; set; } = [];

    public Guid DivisionId { get; set; }

    public EventDivision Division { get; set; } = null!;

    [JsonIgnore] public List<User> Participants { get; set; } = [];

    [JsonIgnore] public List<Participation> Participations { get; set; } = [];

    public int CompareTo(EventTeam? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        if (Score == null) return 1;
        if (other.Score == null) return -1;
        if (Math.Abs(Score.Value - other.Score.Value) < 1e-5) return 0;
        return Score > other.Score ? -1 : 1;
    }

    public override string GetDisplay()
    {
        return Name;
    }
}