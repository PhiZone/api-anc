using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class CollaborationCreationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int InviteeId { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Position { get; set; }
}