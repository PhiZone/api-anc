using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Requests;

public class FeishuTokenDto
{
    [JsonProperty("app_id")]
    public string AppId { get; set; } = null!;
    
    [JsonProperty("app_secret")]
    public string AppSecret { get; set; } = null!;
}