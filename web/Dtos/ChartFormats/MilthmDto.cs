namespace PhiZoneApi.Dtos.ChartFormats;

public class MilthmDto : ChartFormatDto
{
    public dynamic? Content { get; set; }
    public string? Music { get; set; }
    public string? Image { get; set; }
}

public class MilthmMetaDto
{
    public string? Chart { get; set; }
    public string? Music { get; set; }
    public string? Image { get; set; }
}
