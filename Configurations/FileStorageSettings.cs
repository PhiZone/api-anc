namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for File Storage Service.
/// </summary>
public class FileStorageSettings
{
    /// <summary>
    ///     Stores a Client ID, provided by TapTap.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Stores a Client Token, provided by TapTap.
    /// </summary>
    public string ClientToken { get; set; } = null!;

    /// <summary>
    ///     Stores a URL, provided by TapTap at the moment.
    /// </summary>
    public string ServerUrl { get; set; } = null!;
}