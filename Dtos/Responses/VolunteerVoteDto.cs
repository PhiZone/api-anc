namespace PhiZoneApi.Dtos.Responses;

public class VolunteerVoteDto
{
    public Guid Id { get; set; }

    public Guid ChartId { get; set; }

    public double Score { get; set; }

    public string Message { get; set; } = null!;

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}