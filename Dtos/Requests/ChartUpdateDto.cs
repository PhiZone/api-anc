using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ChartUpdateDto
{
    public string? Title { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ChartLevel LevelType { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Level { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double Difficulty { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string AuthorName { get; set; } = null!;

    public string? Illustrator { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsHidden { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsLocked { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsRanked { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<AuthorshipRequestDto> Authorships { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid SongId { get; set; }
}