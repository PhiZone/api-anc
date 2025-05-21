using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class UserEmailAddressUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(6, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCodes.InvalidEmailAddress)]
    [RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = ResponseCodes.InvalidEmailAddress)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string NewEmailAddress { get; set; } = string.Empty;
}