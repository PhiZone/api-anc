using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class ApplicationServiceRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(40, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ServiceTargetType TargetType { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(50000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<string> Parameters { get; set; } = [];

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid ApplicationId { get; set; }
}