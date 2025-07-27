namespace PhiZoneApi.Dtos.Deliverers;

public class PlayInfoTapTapDto
{
    public Guid ChartId { get; set; }

    public Guid ApplicationId { get; set; }

    public string PlayerId { get; set; } = null!;

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public DateTimeOffset EarliestEndTime { get; set; }

    public long Timestamp { get; set; }
}