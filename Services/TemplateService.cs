using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class TemplateService : ITemplateService
{
    private readonly Dictionary<string, TemplateDto> _templates;

    public TemplateService(IOptions<LanguageSettings> settings)
    {
        _templates = new Dictionary<string, TemplateDto>();

        var languageDir = Path.Combine(Directory.GetCurrentDirectory(), settings.Value.DirectoryPath);
        foreach (var language in settings.Value.SupportedLanguages)
        {
            var languageFile = Path.Combine(languageDir, $"{language}.json");
            if (!File.Exists(languageFile)) continue;
            var fileContent = File.ReadAllText(languageFile);
            var json = JsonConvert.DeserializeObject<TemplateDto>(fileContent);
            _templates[language] = json!;
        }
    }

    public Email? GetEmailTemplate(EmailRequestMode mode, string language)
    {
        return _templates[language].Emails.FirstOrDefault(email => email.Mode == mode);
    }

    public string? GetMessage(string key, string language)
    {
        return _templates[language].Messages.FirstOrDefault(message => message.Key == key)?.Content;
    }

    public string ReplacePlaceholders(string template, Dictionary<string, string> dictionary)
    {
        return dictionary.Aggregate(template, (current, pair) => current.Replace($"{{{pair.Key}}}", pair.Value));
    }
}