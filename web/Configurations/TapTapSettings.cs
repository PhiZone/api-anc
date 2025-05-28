namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings provided by TapTap.
/// </summary>
public class TapTapSettings
{
    /// <summary>
    ///     Stores a client ID.
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    ///     Stores a client token.
    /// </summary>
    public string ClientToken { get; set; } = null!;

    /// <summary>
    ///     Stores a URL for TapTap Open API.
    /// </summary>
    public string TapApiUrl { get; set; } = null!;

    /// <summary>
    ///     Stores a URL for File Storage Service.
    /// </summary>
    public string FileStorageUrl { get; set; } = null!;
}