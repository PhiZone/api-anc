using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ChartSubmissionUpdateDto
{
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Title { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 4, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public ChartLevel LevelType { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(20, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Level { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double Difficulty { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(800, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string AuthorName { get; set; } = null!;

    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Illustrator { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsRanked { get; set; }
}