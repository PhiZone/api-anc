using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class AuthorshipRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int AuthorId { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Position { get; set; }
}