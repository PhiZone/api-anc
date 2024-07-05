using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class DiscordTokenDto
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = null!;

    [JsonProperty("token_type")] public string TokenType { get; set; } = null!;

    [JsonProperty("expires_in")] public long ExpiresIn { get; set; }

    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = null!;

    [JsonProperty("scope")] public string Scope { get; set; } = null!;
}