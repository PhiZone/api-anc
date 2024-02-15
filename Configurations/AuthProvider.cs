namespace PhiZoneApi.Configurations;

public class AuthProvider
{
    public string Name { get; set; } = null!;

    public Guid ApplicationId { get; set; }

    public string ClientId { get; set; } = null!;

    public string ClientSecret { get; set; } = null!;

    public string AvatarUrl { get; set; } = null!;

    public string IllustrationUrl { get; set; } = null!;
}