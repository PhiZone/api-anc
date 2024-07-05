using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class CodeRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(6, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Code { get; set; } = string.Empty;
}