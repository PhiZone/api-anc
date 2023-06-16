using PhiZoneApi.Data;
using System.ComponentModel.DataAnnotations;

namespace PhiZoneApi.Dtos
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = ResponseCodes.FieldEmpty), EmailAddress(ErrorMessage = ResponseCodes.InvalidEmailAddress)]
        public required string Email { get; set; }

        [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
        public required string Password { get; set; }
    }
}
