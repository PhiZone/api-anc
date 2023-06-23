namespace PhiZoneApi.Interfaces;

public interface ITemplateService
{
    Dictionary<string, string> GetConfirmationEmailTemplate(string language);
    string ReplacePlaceholders(string template, Dictionary<string, string> dictionary);
}