using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class UserUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(
        @"^([A-Za-z0-9_]{4,24})|([a-zA-Z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{3,12})|([\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{2,12})$",
        ErrorMessage = ResponseCodes.InvalidUserName)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Gender Gender { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
    [LanguageValidator(ErrorMessage = ResponseCodes.UnsupportedLanguage)]
    public string Language { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = ResponseCodes.InvalidRegionCode)]
    [RegionValidator(ErrorMessage = ResponseCodes.UnsupportedRegion)]
    public string RegionCode { get; set; } = null!;

    [DataType(DataType.Date, ErrorMessage = ResponseCodes.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}