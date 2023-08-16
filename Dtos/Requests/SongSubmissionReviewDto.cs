using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class SongSubmissionReviewDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public RequestStatus Status { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsOriginal { get; set; }

    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Message { get; set; }
}