namespace PhiZoneApi.Dtos.Deliverers;

public class PlayInfoDto
{
    public Guid ChartId { get; set; }

    public Guid ConfigurationId { get; set; }

    public Guid ApplicationId { get; set; }

    public int PlayerId { get; set; }

    public DateTimeOffset EarliestEndTime { get; set; }

    public long Timestamp { get; set; }
}