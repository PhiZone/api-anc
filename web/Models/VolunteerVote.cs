namespace PhiZoneApi.Models;

public class VolunteerVote : OwnedResource
{
    public Guid ChartId { get; set; }

    public ChartSubmission Chart { get; set; } = null!;

    public double Score { get; set; }

    public double SuggestedDifficulty { get; set; }

    public string Message { get; set; } = null!;
}