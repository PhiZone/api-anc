using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class RecordCreationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Guid Token { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int MaxCombo { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Perfect { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int GoodEarly { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int GoodLate { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Bad { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public int Miss { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public double StdDeviation { get; set; }

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Hmac { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    [MaxLength(100, ErrorMessage = ResponseCodes.ValueTooLong)]
    public string Checksum { get; set; } = null!;
}