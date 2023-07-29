using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class AuthorshipRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int AuthorId { get; set; }

    public string? Position { get; set; }
}