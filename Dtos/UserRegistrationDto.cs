using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Data;

namespace PhiZoneApi.Dtos;

public class UserRegistrationDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    public required string UserName { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCode.InvalidEmailAddress)]
    public required string Email { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(@"^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$",
        ErrorMessage = ResponseCode.InvalidPassword)]
    public required string Password { get; set; }

    public IFormFile? Avatar { get; set; }

    [Range(0, 2, ErrorMessage = ResponseCode.ValueOutOfRange)] // 0 = Unset; 1 = Male; 2 = Female
    public int Gender { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCode.ValueTooLong)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCode.InvalidLanguageCode)]
    public required string Language { get; set; }

    [DataType(DataType.Date, ErrorMessage = ResponseCode.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}