using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Validators;

public class UserInputValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var resourceService = context.GetRequiredService<IResourceService>();
        return resourceService.IsUserInputValidAsync((string?)value ?? string.Empty, context.MemberName!).Result
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "The input content is prohibited.");
    }
}