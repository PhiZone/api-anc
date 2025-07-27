using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class SongIllustrationDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public IFormFile Song { get; set; } = null!;

    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public IFormFile Illustration { get; set; } = null!;
}