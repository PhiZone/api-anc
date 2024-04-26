namespace PhiZoneApi.Dtos.Responses;

public class PlayResponseDto
{
    public Guid Token { get; set; }

    public long Timestamp { get; set; }
    
    public ChartDto Chart { get; set; } = null!;
}