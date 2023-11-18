using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class UserEmailRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCodes.InvalidEmailAddress)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Email { get; set; } = null!;

    [RegularExpression(
        @"^([a-zA-Z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{3,12})|([\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{2,12})|([A-Za-z0-9_]{4,18})$",
        ErrorMessage = ResponseCodes.InvalidUserName)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? UserName { get; set; }

    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
    [LanguageValidator(ErrorMessage = ResponseCodes.UnsupportedLanguage)]
    public string? Language { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public EmailRequestMode Mode { get; set; }
}