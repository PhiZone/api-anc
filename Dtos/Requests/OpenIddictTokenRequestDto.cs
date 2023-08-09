// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System.ComponentModel.DataAnnotations;
using PhiZoneApi.Constants;

namespace PhiZoneApi.Dtos.Requests;

public class OpenIddictTokenRequestDto
{
    ///<summary>The client's ID, e.g. "regular".</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string client_id { get; set; } = null!;

    ///<summary>The client's secret, e.g. "c29b1587-80f9-475f-b97b-dca1884eb0e3".</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string client_secret { get; set; } = null!;

    ///<summary>The grant type desired, either <c>password</c> or <c>refresh_token</c>.</summary>
    [Required(ErrorMessage = ResponseCodes.FieldEmpty)]
    public string grant_type { get; set; } = null!;

    ///<summary>The user's email address, e.g. "contact@phi.zone", when the grant type is <c>password</c>.</summary>
    public string? username { get; set; }

    ///<summary>The user's password, when the grant type is <c>password</c>.</summary>
    public string? password { get; set; }

    ///<summary>The user's refresh token, when the grant type is <c>refresh_token</c>.</summary>
    public string? refresh_token { get; set; }

    public string? user_id { get; set; }
}