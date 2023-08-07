using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Dtos.Requests;

public class ChartAssetUpdateDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [Range(0, 4, ErrorMessage = ResponseCodes.ValueOutOfRange)]
    public ChartAssetType Type { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(1000, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Name { get; set; } = null!;
}