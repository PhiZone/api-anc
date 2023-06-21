namespace PhiZoneApi.Dtos;

public class CoolDownResponseDto
{
    public int Status = 3;
    public required string Code { get; set; }
    public required DateTimeOffset DateAvailable { get; set; }
}