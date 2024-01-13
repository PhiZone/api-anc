using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Models;

public class Record : LikeableResource, IComparable<Record>
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

    public double StdDeviation { get; set; }

    public double Rks { get; set; }

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public Guid ApplicationId { get; set; }

    public Application Application { get; set; } = null!;

    public override string GetDisplay()
    {
        return $"{Score} {Accuracy:P2}";
    }

    public double GetScore()
    {
        return Rks;
    }

    public int CompareTo(Record? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        if (Math.Abs(Rks - other.Rks) < 1e-5) return DateCreated.CompareTo(other.DateCreated);
        return Score > other.Score ? -1 : 1;
    }
}