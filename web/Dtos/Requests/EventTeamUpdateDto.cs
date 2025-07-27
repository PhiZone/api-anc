using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class EventTeamUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(40, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Name { get; set; } = null!;

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Description { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int ClaimedParticipantCount { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public int ClaimedSubmissionCount { get; set; }
}