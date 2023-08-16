using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class PetObjectiveAnswerDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<int> Choices { get; set; } = null!;
}