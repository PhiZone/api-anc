namespace PhiZoneApi.Configurations;

/// <summary>
///     Stores settings for multilingual functions.
/// </summary>
public class LanguageSettings
{
    /// <summary>
    ///     Stores a path to language files.
    /// </summary>
    public string DirectoryPath { get; set; } = null!;

    /// <summary>
    ///     Determines all supported languages for this app.
    /// </summary>
    public List<string> SupportedLanguages { get; set; } = null!;
}