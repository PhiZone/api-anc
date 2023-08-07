using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class UserPasswordResetDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(6, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$",
        ErrorMessage = ResponseCodes.InvalidPassword)]
    public string Password { get; set; } = null!;
}