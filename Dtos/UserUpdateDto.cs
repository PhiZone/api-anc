using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos;

public class UserUpdateDto
{
    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(
        @"^([a-zA-Z0-9_\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{3,12})|([\u4e00-\u9fa5\u3040-\u309f\u30a0-\u30ff\uac00-\ud7af]{2,12})|([A-Za-z0-9_]{4,18})$",
        ErrorMessage = ResponseCode.InvalidUserName)]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [Range(0, 2, ErrorMessage = ResponseCode.ValueOutOfRange)]
    public int Gender { get; set; }

    [MaxLength(2000, ErrorMessage = ResponseCode.ValueTooLong)]
    public string? Biography { get; set; }

    [Required(ErrorMessage = ResponseCode.FieldEmpty)]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$", ErrorMessage = ResponseCode.InvalidLanguageCode)]
    public string Language { get; set; } = null!;

    [DataType(DataType.Date, ErrorMessage = ResponseCode.InvalidDate)]
    public DateTimeOffset? DateOfBirth { get; set; }
}