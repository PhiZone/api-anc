using System.ComponentModel.DataAnnotations;
using Ganss.Xss;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;

namespace PhiZoneApi.Validators;

public class UserInputValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var resources = JsonConvert.DeserializeObject<ResourceDto>(File.ReadAllText(
            Path.Combine(Directory.GetCurrentDirectory(),
                configuration.GetSection("ResourceSettings").GetValue<string>("DirectoryPath")!, "resources.json")))!;

        if (value == null) return ValidationResult.Success;

        // ReSharper disable once InvertIf
        if (((string)value).Contains("<") || ((string)value).Contains(">"))
        {
            var sanitizer = new HtmlSanitizer();
            if (sanitizer.Sanitize((string)value) != (string)value)
                return new ValidationResult(ErrorMessage ?? "The input content is prohibited.");
        }

        return resources.ProhibitedWords.Any(word => ((string)value).ToLower().Contains(word))
            ? new ValidationResult(ErrorMessage ?? "The input content is prohibited.")
            : ValidationResult.Success;
    }
}