using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class PetSubjectiveAnswerDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Answer1 { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Answer2 { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Answer3 { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Chart { get; set; } = string.Empty;
}