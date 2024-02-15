namespace PhiZoneApi.Dtos.Responses;

public class ReplyDto
{
    public Guid Id { get; set; }

    public Guid CommentId { get; set; }

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;

    public int LikeCount { get; set; }

    public int OwnerId { get; set; }

    public UserDto Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}