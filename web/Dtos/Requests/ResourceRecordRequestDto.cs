using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ResourceRecordRequestDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public ResourceRecordType Type { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 5, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public EditionType EditionType { get; set; }

    [MaxLength(200, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Edition { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1600, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string AuthorName { get; set; } = null!;

    [MaxLength(4000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Description { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 4, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public ResourceRecordStrategy Strategy { get; set; }

    [Url(ErrorMessage = ResponseCodes.InvalidUrl)]
    [MaxLength(4000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string? Media { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Url(ErrorMessage = ResponseCodes.InvalidUrl)]
    [MaxLength(4000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Source { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1600, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string CopyrightOwner { get; set; } = null!;
}