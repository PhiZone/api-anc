namespace PhiZoneApi.Dtos.Requests;

public class CommentCreationDto
{
    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;
}