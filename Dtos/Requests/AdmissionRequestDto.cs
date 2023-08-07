using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class AdmissionRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid AdmitterId { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Label { get; set; }
}