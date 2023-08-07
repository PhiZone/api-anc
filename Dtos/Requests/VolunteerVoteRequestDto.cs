using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class VolunteerVoteRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(-3, 3, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Score { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Message { get; set; } = null!;
}