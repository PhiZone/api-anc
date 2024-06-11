using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class EventTaskRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(40, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 4, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public EventTaskType Type { get; set; }

    [MaxLength(50000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Code { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsHidden { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Description { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid DivisionId { get; set; }

    public DateTimeOffset? DateExecuted { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public DateTimeOffset DateCreated { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public DateTimeOffset DateUpdated { get; set; }
}