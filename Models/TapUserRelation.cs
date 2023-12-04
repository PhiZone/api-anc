namespace PhiZoneApi.Models;

public class TapUserRelation
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ApplicationId { get; set; }

    public Application Application { get; set; } = null!;

    public string UnionId { get; set; } = null!;
}