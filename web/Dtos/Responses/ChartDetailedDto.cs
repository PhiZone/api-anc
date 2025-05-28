namespace PhiZoneApi.Dtos.Responses;

public class ChartDetailedDto : ChartDto
{
    public int? PersonalBestScore { get; set; }

    public double? PersonalBestAccuracy { get; set; }

    public double? PersonalBestRks { get; set; }
}