namespace PhiZoneApi.Dtos;

/// <summary>
///     A DTO specially made for Swagger, just to ensure that the documentation is working properly.
/// </summary>
public class OpenIddictRevocationRequestDto
{
    ///<summary>The client's ID, e.g. "regular". Strictly Required.</summary>
    public string? client_id { get; set; }

    ///<summary>The client's secret, e.g. "c29b1587-80f9-475f-b97b-dca1884eb0e3". Strictly Required.</summary>
    public string? client_secret { get; set; }
    
    ///<summary>The user's refresh token. Strictly Required.</summary>
    public string? token { get; set; }
}