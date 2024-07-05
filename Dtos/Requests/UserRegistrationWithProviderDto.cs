using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class UserRegistrationWithProviderDto
{
    [Range(0, 3, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Gender Gender { get; set; } = Gender.Unset;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
    [LanguageValidator(ErrorMessage = ResponseCodes.UnsupportedLanguage)]
    public string Language { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = ResponseCodes.InvalidRegionCode)]
    [RegionValidator(ErrorMessage = ResponseCodes.UnsupportedRegion)]
    public string RegionCode { get; set; } = string.Empty;

    [DataType(DataType.Date, ErrorMessage = ResponseCodes.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}