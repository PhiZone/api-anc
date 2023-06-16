using PhiZoneApi.Data;
using System.ComponentModel.DataAnnotations;

namespace PhiZoneApi.Dtos
{
    public class UserRegistrationDto
    {
        [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
        public required string UserName { get; set; }

        [Required(ErrorMessage = ResponseCodes.FieldEmpty), EmailAddress(ErrorMessage = ResponseCodes.InvalidEmailAddress)]
        public required string Email { get; set; }

        [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
        [RegularExpression(@"^(?=.*[^a-zA-Z0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9]).{6,18}$", ErrorMessage = ResponseCodes.InvalidPassword)]
        public required string Password { get; set; }

        public IFormFile? Avatar { get; set; }

        [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)] // 0 = Unset; 1 = Male; 2 = Female
        public int Gender { get; set; }

        [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
        public string? Biography { get; set; }

        [Required(ErrorMessage = ResponseCodes.FieldEmpty), RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", ErrorMessage = ResponseCodes.InvalidLanguageCode)]
        public required string Language { get; set; }

        [DataType(DataType.Date, ErrorMessage = ResponseCodes.InvalidDate)]
        public DateTime? DateOfBirth { get; set; }
    }
}
