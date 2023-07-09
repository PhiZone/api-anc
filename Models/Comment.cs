namespace PhiZoneApi.Models;

public class Comment : Interaction
{
    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;
}