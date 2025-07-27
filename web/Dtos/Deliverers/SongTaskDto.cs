namespace PhiZoneApi.Dtos.Deliverers;

public class SongTaskDto
{
    public Guid SongId { get; set; }

    public bool IsSubmission { get; set; }

    public bool Burn { get; set; }

    public byte[] Body { get; set; } = null!;
}