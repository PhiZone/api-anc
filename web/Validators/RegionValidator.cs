using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Validators;

public class RegionValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        var regionRepository = context.GetRequiredService<IRegionRepository>();
        return value != null && !regionRepository.RegionExists((string)value)
            ? new ValidationResult(ErrorMessage ?? "The region is not supported.")
            : ValidationResult.Success;
    }
}