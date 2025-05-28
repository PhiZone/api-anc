namespace PhiZoneApi.Models;

public class Participation
{
    public Guid TeamId { get; set; }

    public EventTeam Team { get; set; } = null!;

    public int ParticipantId { get; set; }

    public User Participant { get; set; } = null!;

    public string? Position { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}