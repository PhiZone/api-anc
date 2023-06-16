using PhiZoneApi.Data;
using System.ComponentModel.DataAnnotations;

namespace PhiZoneApi.Dtos
{
    public class UserUpdateDto
    {
        public string? UserName { get; set; }

        public IFormFile? Avatar { get; set; }

        [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
        public int? Gender { get; set; }

        [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
        public string? Biography { get; set; }

        [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
        public string? Language { get; set; }

        [DataType(DataType.Date, ErrorMessage = ResponseCodes.InvalidDate)]
        public DateTime? DateOfBirth { get; set; }
    }
}
