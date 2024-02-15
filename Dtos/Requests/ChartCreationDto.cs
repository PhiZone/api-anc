using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class ChartCreationDto
{
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Title { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 4, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public ChartLevel LevelType { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(20, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Level { get; set; } = string.Empty;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double Difficulty { get; set; }

    public IFormFile? File { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(800, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string AuthorName { get; set; } = string.Empty;

    public IFormFile? Illustration { get; set; }

    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Illustrator { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsHidden { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsLocked { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsRanked { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<string> Tags { get; set; } = [];

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid SongId { get; set; }
}