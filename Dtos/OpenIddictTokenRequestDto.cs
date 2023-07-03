namespace PhiZoneApi.Dtos;

/// <summary>
///     A DTO specially made for Swagger, just to ensure that the documentation is working properly.
/// </summary>
public class OpenIddictTokenRequestDto
{
    ///<summary>The client's ID, e.g. "regular". Strictly Required.</summary>
    public string? client_id { get; set; }

    ///<summary>The client's secret, e.g. "c29b1587-80f9-475f-b97b-dca1884eb0e3". Strictly Required.</summary>
    public string? client_secret { get; set; }

    ///<summary>The grant type desired, either <c>password</c> or <c>refresh_token</c>. Strictly Required.</summary>
    public string? grant_type { get; set; }

    ///<summary>The user's email address, e.g. "contact@phi.zone", when the grant type is <c>password</c>.</summary>
    public string? username { get; set; }

    ///<summary>The user's password, when the grant type is <c>password</c>.</summary>
    public string? password { get; set; }

    ///<summary>The user's refresh token, when the grant type is <c>refresh_token</c>.</summary>
    public string? refresh_token { get; set; }
}