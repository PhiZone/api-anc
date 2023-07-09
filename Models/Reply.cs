namespace PhiZoneApi.Models;

public class Reply : LikeableResource
{
    public Guid CommentId { get; set; }

    public Comment Comment { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;
}