namespace PhiZoneApi.Models;

public class VolunteerVote : Resource
{
    public Guid ChartId { get; set; }

    public ChartSubmission Chart { get; set; } = null!;

    public double Score { get; set; }

    public string Message { get; set; } = null!;
}