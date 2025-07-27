// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class OpenIddictRevocationRequestDto
{
    ///<summary>The client's ID, e.g. "p".</sublicummary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string client_id { get; set; } = null!;

    ///<summary>The client's secret.</summary>
    public string? client_secret { get; set; }

    ///<summary>The user's refresh token.</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string token { get; set; } = null!;
}