using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Responses;

public class OpenIddictErrorDto
{
    [JsonProperty("error")] public string Error { get; set; } = null!;

    [JsonProperty("error_description")] public string ErrorDescription { get; set; } = null!;

    [JsonProperty("error_uri")] public string ErrorUri { get; set; } = null!;
}