using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Data;

namespace PhiZoneApi.Dtos;

public class UserLoginDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCode.InvalidEmailAddress)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    public string Password { get; set; } = null!;
}