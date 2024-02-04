namespace PhiZoneApi.Models;

public class ApplicationUser
{
    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid ApplicationId { get; set; }

    public Application Application { get; set; } = null!;

    public string? UnionId { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTimeOffset? DateAccessTokenExpires { get; set; }

    public DateTimeOffset? DateRefreshTokenExpires { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}