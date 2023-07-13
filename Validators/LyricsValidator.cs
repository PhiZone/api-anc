using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PhiZoneApi.Validators;

public partial class LyricsValidator : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        return value != null && !LrcRegex().IsMatch((string)value)
            ? new ValidationResult(ErrorMessage ?? "The lyrics do not conform to Format LRC.")
            : ValidationResult.Success;
    }

    [GeneratedRegex(
        "\\[(\\d{2,}:\\d{2}\\.\\d{2})\\][^\r\n]*|\\[((al|ar|au|by|re|ti|ve):[^[\\]]*|offset:(\\+|-)\\d+)\\]")]
    private static partial Regex LrcRegex();
}