using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class ApplicationCreationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(40, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Name { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public IFormFile Illustration { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Illustrator { get; set; } = null!;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Description { get; set; }


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Homepage { get; set; } = null!;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? ApiEndpoint { get; set; }


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 6, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public ApplicationType Type { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Secret { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int OwnerId { get; set; }
}