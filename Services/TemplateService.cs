using LC.Newtonsoft.Json;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class TemplateService : ITemplateService
{
    private readonly Dictionary<string, Dictionary<string, string>> _registrationEmail;

    public TemplateService(IOptions<LanguageSettings> settings)
    {
        _registrationEmail = new Dictionary<string, Dictionary<string, string>>();

        var languageDir = Path.Combine(Directory.GetCurrentDirectory(), settings.Value.DirectoryPath);
        foreach (var language in settings.Value.SupportedLanguages)
        {
            var languageFile = Path.Combine(languageDir, $"{language}.json");
            if (!File.Exists(languageFile)) continue;
            var fileContent = File.ReadAllText(languageFile);
            var json = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContent);
            _registrationEmail[language] = json["RegistrationEmail"];
        }
    }

    public Dictionary<string, string> GetRegistrationEmailTemplate(string language)
    {
        return _registrationEmail[language];
    }

    public string ReplacePlaceholders(string template, Dictionary<string, string> dictionary)
    {
        var result = template;
        foreach (var pair in dictionary) result = result.Replace($"{{{pair.Key}}}", pair.Value);
        return result;
    }
}