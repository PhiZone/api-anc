using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos;

public class UserRegistrationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(
        @"^([a-zA-Z0-9_\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{3,12})|([\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{2,12})|([A-Za-z0-9_]{4,18})$",
        ErrorMessage = ResponseCodes.InvalidUserName)]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCodes.InvalidEmailAddress)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$",
        ErrorMessage = ResponseCodes.InvalidPassword)]
    public string Password { get; set; } = null!;

    public IFormFile? Avatar { get; set; }

    [Range(0, 3, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Gender Gender { get; set; } = Gender.Unset;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
    public string Language { get; set; } = null!;

    [DataType(DataType.Date, ErrorMessage = ResponseCodes.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}