using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ApplicationCreationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Name { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public IFormFile Illustration { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Homepage { get; set; } = null!;

    public string? ApiEndpoint { get; set; }


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ApplicationType Type { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Secret { get; set; } = null!;


    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int OwnerId { get; set; }
}