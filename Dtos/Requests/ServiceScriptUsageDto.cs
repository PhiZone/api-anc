using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class ServiceScriptUsageDto
{
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public Dictionary<string, string> Parameters { get; set; } = null!;
}