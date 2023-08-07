using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class AuthorshipRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int AuthorId { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Position { get; set; }
}