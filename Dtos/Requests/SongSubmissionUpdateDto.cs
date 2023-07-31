using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class SongSubmissionUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public EditionType EditionType { get; set; }

    public string? Edition { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string AuthorName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Accessibility Accessibility { get; set; }


    [LyricsValidator(ErrorMessage = ResponseCodes.UnsupportedLyricsFormat)]
    public string? Lyrics { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Bpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int MinBpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int MaxBpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Offset { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public TimeSpan PreviewStart { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public TimeSpan PreviewEnd { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<AuthorshipRequestDto> Authorships { get; set; } = null!;
}