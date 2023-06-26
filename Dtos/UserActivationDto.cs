using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos;

public class UserActivationDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    public string Code { get; set; } = null!;
}