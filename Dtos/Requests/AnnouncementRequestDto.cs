using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class AnnouncementRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(30000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Content { get; set; } = null!;
}