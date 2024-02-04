using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class VolunteerVoteRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(-3, 3, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double Score { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double SuggestedDifficulty { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Message { get; set; } = string.Empty;
}