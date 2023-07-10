namespace PhiZoneApi.Models;

public class Record : LikeableResource
{
    public Guid ChartId { get; set; }

    public Chart Chart { get; set; } = null!;

    public int Score { get; set; }

    public double Accuracy { get; set; }

    public bool IsFullCombo { get; set; }

    public int MaxCombo { get; set; }

    public int Perfect { get; set; }

    public int GoodEarly { get; set; }

    public int GoodLate { get; set; }

    public int Bad { get; set; }

    public int Miss { get; set; }

    public double Rks { get; set; }

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public Guid ApplicationId { get; set; }

    public Application Application { get; set; } = null!;
}