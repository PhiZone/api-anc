using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Models;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class UserUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(
        @"^([a-zA-Z0-9_\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{3,12})|([\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{2,12})|([A-Za-z0-9_]{4,18})$",
        ErrorMessage = ResponseCodes.InvalidUserName)]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Gender { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
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