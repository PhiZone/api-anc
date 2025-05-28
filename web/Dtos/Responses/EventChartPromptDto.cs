namespace PhiZoneApi.Dtos.Responses;

public class EventChartPromptDto : ChartDto
{
    public string? Label { get; set; }

    public string? EventDescription { get; set; }
}