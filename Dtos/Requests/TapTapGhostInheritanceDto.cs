using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class TapTapGhostInheritanceDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(4, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Code { get; set; } = string.Empty;
}