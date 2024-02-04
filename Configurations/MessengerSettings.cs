namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for Messenger Service.
/// </summary>
public class MessengerSettings
{
    /// <summary>
    ///     Stores a URL for Messenger API.
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