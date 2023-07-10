namespace PhiZoneApi.Dtos.Requests;

public class RecordCreationDto
{
    public Guid ChartId { get; set; }

    public int MaxCombo { get; set; }

    public int Perfect { get; set; }

    public int GoodEarly { get; set; }

    public int GoodLate { get; set; }

    public int Bad { get; set; }

    public int Miss { get; set; }

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public int OwnerId { get; set; }

    public Guid ApplicationId { get; set; }
}