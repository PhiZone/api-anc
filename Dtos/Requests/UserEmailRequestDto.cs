using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class UserEmailRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public EmailRequestMode Mode { get; set; }
}