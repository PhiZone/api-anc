namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for Meilisearch.
/// </summary>
public class MeilisearchSettings
{
    /// <summary>
    ///     Stores a URL for Meilisearch API.
    /// </summary>
    public string ApiUrl { get; set; } = null!;

    /// <summary>
    ///     Stores a master key.
    /// </summary>
    public string MasterKey { get; set; } = null!;
}