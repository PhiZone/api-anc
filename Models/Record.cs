namespace PhiZoneApi.Models;

public class Record
{
    public Guid Id { get; set; }

    public int PlayerId { get; set; }

    public User Player { get; set; } = null!;

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

    public DateTimeOffset DateCreated { get; set; }
}