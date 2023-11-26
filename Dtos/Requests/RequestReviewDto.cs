using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class RequestReviewDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool Approve { get; set; }
}