namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores secrets and settings for Feishu Service.
/// </summary>
public class FeishuSettings
{
    /// <summary>
    ///     Stores a URL for Feishu Open API.
    /// </summary>
    public string ApiUrl { get; set; } = null!;

    /// <summary>
    ///     Stores an app ID.
    /// </summary>
    public string AppId { get; set; } = null!;

    /// <summary>
    ///     Stores an app secret.
    /// </summary>
    public string AppSecret { get; set; } = null!;

    /// <summary>
    ///     Stores card IDs.
    /// </summary>
    public List<string> Cards { get; set; } = null!;

    /// <summary>
    ///     Stores chat IDs.
    /// </summary>
    public List<string> Chats { get; set; } = null!;
}