using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class TemplateService : ITemplateService
{
    private readonly Dictionary<string, Dictionary<string, string>> _confirmationEmail;
    private readonly Dictionary<string, Dictionary<string, string>> _passwordResetEmail;

    public TemplateService(IOptions<LanguageSettings> settings)
    {
        _confirmationEmail = new Dictionary<string, Dictionary<string, string>>();
        _passwordResetEmail = new Dictionary<string, Dictionary<string, string>>();

        var languageDir = Path.Combine(Directory.GetCurrentDirectory(), settings.Value.DirectoryPath);
        foreach (var language in settings.Value.SupportedLanguages)
        {
            var languageFile = Path.Combine(languageDir, $"{language}.json");
            if (!File.Exists(languageFile)) continue;
            var fileContent = File.ReadAllText(languageFile);
            var json = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContent);
            _confirmationEmail[language] = json!["ConfirmationEmail"];
            _passwordResetEmail[language] = json["PasswordResetEmail"];
        }
    }

    public Dictionary<string, string> GetEmailTemplate(EmailRequestMode mode, string language)
    {
        return mode switch
        {
            EmailRequestMode.EmailConfirmation => _confirmationEmail[language],
            EmailRequestMode.PasswordReset => _passwordResetEmail[language],
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public string ReplacePlaceholders(string template, Dictionary<string, string> dictionary)
    {
        var result = template;
        foreach (var pair in dictionary) result = result.Replace($"{{{pair.Key}}}", pair.Value);
        return result;
    }
}