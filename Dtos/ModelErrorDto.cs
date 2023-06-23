namespace PhiZoneApi.Dtos;

public class ModelErrorDto
{
    public string Field { get; set; } = null!;

    public ICollection<string>? Errors { get; set; }
}