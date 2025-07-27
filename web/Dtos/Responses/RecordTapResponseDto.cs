namespace PhiZoneApi.Dtos.Responses;

public class RecordTapResponseDto
{
    public Guid Id { get; set; }

    public int Score { get; set; }

    public double Accuracy { get; set; }

    public bool IsFullCombo { get; set; }

    public ulong ExperienceDelta { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}