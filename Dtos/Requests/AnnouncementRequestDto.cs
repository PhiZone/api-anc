using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class AnnouncementRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Content { get; set; } = null!;
}