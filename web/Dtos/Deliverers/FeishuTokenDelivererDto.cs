using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class FeishuTokenDelivererDto
{
    public int Code { get; set; }

    [JsonProperty("msg")] public string Message { get; set; } = null!;

    [JsonProperty("tenant_access_token")] public string TenantAccessToken { get; set; } = null!;

    public int Expire { get; set; }
}