using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class QqOpenIdDto
{
    [JsonProperty("client_id")] public string ClientId { get; set; } = null!;

    [JsonProperty("openid")] public string OpenId { get; set; } = null!;
}