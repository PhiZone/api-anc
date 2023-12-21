using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class VoteRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Arrangement { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Gameplay { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int VisualEffects { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Creativity { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Concord { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Impression { get; set; }
}