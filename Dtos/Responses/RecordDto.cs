namespace PhiZoneApi.Dtos.Responses;

public class RecordDto
{
    public Guid Id { get; set; }

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

    public int? Position { get; set; }

    public int PerfectJudgment { get; set; }

    public int GoodJudgment { get; set; }

    public Guid ApplicationId { get; set; }

    public Guid ChartId { get; set; }

    public ChartDto? Chart { get; set; }

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public UserDto Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}