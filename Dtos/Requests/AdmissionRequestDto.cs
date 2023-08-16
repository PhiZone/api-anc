using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class AdmissionRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid AdmitterId { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Label { get; set; }
}