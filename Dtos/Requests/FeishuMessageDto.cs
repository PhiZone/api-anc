using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Requests;

public class FeishuMessageDto
{
    [JsonProperty("receive_id")] public string ReceiveId { get; set; } = null!;

    [JsonProperty("msg_type")] public string MessageType { get; set; } = null!;

    [JsonProperty("content")] public string Content { get; set; } = null!;

    // public string Uuid { get; set; } = null!;
}