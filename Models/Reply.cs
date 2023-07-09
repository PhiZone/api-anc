namespace PhiZoneApi.Models;

public class Reply
{
    public Guid Id { get; set; }

    public Guid CommentId { get; set; }

    public Comment Comment { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }
}