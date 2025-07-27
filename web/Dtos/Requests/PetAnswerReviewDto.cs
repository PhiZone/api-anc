using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class PetAnswerReviewDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 60, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int Score { get; set; }
}