using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
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
        return value != null && resources.ProhibitedWords.Any(word => ((string)value).ToLower().Contains(word))
            ? new ValidationResult(ErrorMessage ?? "The input content is prohibited.")
            : ValidationResult.Success;
    }
}