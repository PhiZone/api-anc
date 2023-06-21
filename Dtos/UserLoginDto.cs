using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Data;

namespace PhiZoneApi.Dtos;

public class UserLoginDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCode.InvalidEmailAddress)]
    public required string Email { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    public required string Password { get; set; }
}