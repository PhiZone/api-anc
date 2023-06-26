using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos;

public class UserRegistrationDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [EmailAddress(ErrorMessage = ResponseCode.InvalidEmailAddress)]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(@"^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$",
        ErrorMessage = ResponseCode.InvalidPassword)]
    public string Password { get; set; } = null!;

    public IFormFile? Avatar { get; set; }

    /// <summary>
    ///     0: Unset, 1: Male, 2: Female, 3: Other
    /// </summary>
    [Range(0, 3, ErrorMessage = ResponseCode.ValueOutOfRange)]
    public Gender Gender { get; set; } = Gender.Unset;

    [MaxLength(2000, ErrorMessage = ResponseCode.ValueTooLong)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCode.InvalidLanguageCode)]
    public string Language { get; set; } = null!;

    [DataType(DataType.Date, ErrorMessage = ResponseCode.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}