namespace PhiZoneApi.Configurations;

public class LanguageSettings
{
    public required string DirectoryPath { get; set; }

    public required List<string> SupportedLanguages { get; set; }
}