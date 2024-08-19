namespace PhiZoneApi.Dtos.Deliverers;

public class TapGhost
{
    public Guid ApplicationId { get; set; }

    public string UnionId { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public ulong Experience { get; set; }

    public double Rks { get; set; }

    public DateTimeOffset DateLastLoggedIn { get; set; }

    public DateTimeOffset DateJoined { get; set; }
}