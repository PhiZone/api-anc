namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for the Phigrim API.
/// </summary>
public class PhigrimSettings
{
    /// <summary>
    ///     Stores a URL for the Phigrim API.
    /// </summary>
    public string ApiUrl { get; set; } = null!;

    /// <summary>
    ///     Stores a client ID.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Stores a client secret.
    /// </summary>
    public string ClientSecret { get; set; } = null!;
}