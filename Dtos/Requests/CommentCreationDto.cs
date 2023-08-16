using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class CommentCreationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(3000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Content { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
    [LanguageValidator(ErrorMessage = ResponseCodes.UnsupportedLanguage)]
    public string Language { get; set; } = null!;
}