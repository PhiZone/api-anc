using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ChartSubmissionCreationDto
{
    public string? Title { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ChartLevel LevelType { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Level { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double Difficulty { get; set; }

    public IFormFile? File { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string AuthorName { get; set; } = null!;

    public IFormFile? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Accessibility Accessibility { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsRanked { get; set; }

    public Guid? SongId { get; set; }

    public Guid? SongSubmissionId { get; set; }
}