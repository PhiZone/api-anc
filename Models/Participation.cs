namespace PhiZoneApi.Models;

public class Participation
{
    public Guid EventTeamId { get; set; }

    public EventTeam EventTeam { get; set; } = null!;

    public int ParticipantId { get; set; }

    public User Participant { get; set; } = null!;

    public string? Position { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}