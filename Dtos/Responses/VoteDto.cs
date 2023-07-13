namespace PhiZoneApi.Dtos.Responses;

public class VoteDto
{
    public Guid Id { get; set; }

    public Guid ChartId { get; set; }

    public int Arrangement { get; set; }

    public int Feel { get; set; }

    public int VisualEffects { get; set; }

    public int Creativity { get; set; }

    public int Concord { get; set; }

    public int Impression { get; set; }

    public int Total { get; set; }

    public double Multiplier { get; set; }

    public int OwnerId { get; set; }

    public DateTimeOffset DateCreated { get; set; }
}