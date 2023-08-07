// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class OpenIddictRevocationRequestDto
{
    ///<summary>The client's ID, e.g. "regular".</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string client_id { get; set; } = null!;

    ///<summary>The client's secret, e.g. "c29b1587-80f9-475f-b97b-dca1884eb0e3".</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string client_secret { get; set; } = null!;

    ///<summary>The user's refresh token.</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string token { get; set; } = null!;
}