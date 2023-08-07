using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class UserEmailRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public EmailRequestMode Mode { get; set; }
}