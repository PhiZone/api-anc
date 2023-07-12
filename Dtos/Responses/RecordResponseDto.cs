namespace PhiZoneApi.Dtos.Responses;

public class RecordResponseDto
{
    public Guid Id { get; set; }

    public int Score { get; set; }

    public double Accuracy { get; set; }

    public bool IsFullCombo { get; set; }

    public int ExperienceDelta { get; set; }

    public double RksBefore { get; set; }

    public double RksAfter { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}