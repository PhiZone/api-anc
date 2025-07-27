namespace PhiZoneApi.Dtos.Responses;

public class ModelErrorDto
{
    public string Field { get; set; } = null!;

    public ICollection<string>? Errors { get; set; }
}