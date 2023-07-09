using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ChapterUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Subtitle { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }
    
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Accessibility Accessibility { get; set; }
    
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsHidden { get; set; }
    
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsLocked { get; set; }
}