using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Validators;

namespace PhiZoneApi.Dtos.Requests;

public class SongUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public EditionType EditionType { get; set; }

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Edition { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(800, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string AuthorName { get; set; } = null!;

    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? FileChecksum { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string Illustrator { get; set; } = null!;

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


    [LyricsValidator(ErrorMessage = ResponseCodes.UnsupportedLyricsFormat)]
    [MaxLength(20000, ErrorMessage = ResponseCodes.ValueTooLong)]
    [UserInputValidator(ErrorMessage = ResponseCodes.ContentProhibited)]
    public string? Lyrics { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, double.MaxValue, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double Bpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, double.MaxValue, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double MinBpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, double.MaxValue, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public double MaxBpm { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Offset { get; set; }

    public IFormFile? License { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public bool IsOriginal { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public TimeSpan PreviewStart { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public TimeSpan PreviewEnd { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public List<string> Tags { get; set; } = [];
}