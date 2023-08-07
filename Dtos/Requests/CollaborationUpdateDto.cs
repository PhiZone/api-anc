using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class CollaborationUpdateDto
{
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Position { get; set; }
}