using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface ITemplateService
{
    Dictionary<string, string> GetEmailTemplate(EmailRequestMode mode, string language);

    string ReplacePlaceholders(string template, Dictionary<string, string> dictionary);
}