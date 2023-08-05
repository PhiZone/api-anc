namespace PhiZoneApi.Dtos.Responses;

public class RecordResponseDto
{
    public Guid Id { get; set; }

    public int Score { get; set; }

    public double Accuracy { get; set; }

    public bool IsFullCombo { get; set; }

    public UserDto Player { get; set; } = null!;

    public int ExperienceDelta { get; set; }

    public double RksBefore { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}