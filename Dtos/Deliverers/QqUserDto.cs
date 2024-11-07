using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class QqUserDto
{
    [JsonProperty("ret")] public string Ret { get; set; } = null!;

    [JsonProperty("msg")] public string Message { get; set; } = null!;

    [JsonProperty("nickname")] public string Nickname { get; set; } = null!;

    [JsonProperty("figureurl")] public string FigureUrl { get; set; } = null!;

    [JsonProperty("figureurl_1")] public string FigureUrl1 { get; set; } = null!;

    [JsonProperty("figureurl_2")] public string FigureUrl2 { get; set; } = null!;

    [JsonProperty("figureurl_qq_1")] public string FigureUrlQq1 { get; set; } = null!;

    [JsonProperty("figureurl_qq_2")] public string FigureUrlQq2 { get; set; } = null!;
}