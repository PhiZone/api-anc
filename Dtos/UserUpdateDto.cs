using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos;

public class UserUpdateDto
{
    public string? UserName { get; set; }

    public IFormFile? Avatar { get; set; }

    [Range(0, 2, ErrorMessage = ResponseCode.ValueOutOfRange)]
    public int? Gender { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCode.ValueTooLong)]
    public string? Biography { get; set; }

    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCode.InvalidLanguageCode)]
    public string? Language { get; set; }

    [DataType(DataType.Date, ErrorMessage = ResponseCode.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}