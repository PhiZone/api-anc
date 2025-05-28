using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Interfaces;

public interface ITemplateService
{
    Email? GetEmailTemplate(EmailRequestMode mode, string language);

    string? GetMessage(string key, string language);

    string ReplacePlaceholders(string template, Dictionary<string, string> dictionary);
}