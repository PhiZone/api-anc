namespace PhiZoneApi.Dtos;

public class ModelErrorDto
{
    public required string Field { get; set; }
    public ICollection<string>? Errors { get; set; }
}