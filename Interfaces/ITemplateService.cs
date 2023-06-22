namespace PhiZoneApi.Interfaces;

public interface ITemplateService
{
    Dictionary<string, string> GetRegistrationEmailTemplate(string language);
    string ReplacePlaceholders(string template, Dictionary<string, string> dictionary);
}