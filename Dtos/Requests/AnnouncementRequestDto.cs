using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class AnnouncementRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(30000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Content { get; set; } = null!;
}