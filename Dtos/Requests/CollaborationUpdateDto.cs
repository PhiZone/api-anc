using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class CollaborationUpdateDto
{
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Position { get; set; }
}