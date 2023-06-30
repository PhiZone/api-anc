using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;

namespace PhiZoneApi.Validators;

public class LanguageValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var options = context.GetRequiredService<IOptions<LanguageSettings>>();
        return !options.Value.SupportedLanguages.Contains(value)
            ? new ValidationResult(ErrorMessage ?? "The language is not supported.")
            : ValidationResult.Success;
    }
}