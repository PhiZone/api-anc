using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Responses;

/// <summary>
///     A DTO specially made for Swagger, just to ensure that the documentation is working properly.
/// </summary>
public class OpenIddictErrorDto
{
    [JsonProperty("error")] public string Error { get; set; } = null!;

    [JsonProperty("error_description")] public string ErrorDescription { get; set; } = null!;

    [JsonProperty("error_uri")] public string ErrorUri { get; set; } = null!;
}