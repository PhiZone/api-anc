namespace PhiZoneApi.Dtos.Requests;

public class VolunteerVoteRequestDto
{
    public Guid ChartId { get; set; }

    public int Score { get; set; }

    public string Message { get; set; } = null!;
}