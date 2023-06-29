using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos;

public class UserEmailRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public EmailRequestMode Mode { get; set; }
}