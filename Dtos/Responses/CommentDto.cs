namespace PhiZoneApi.Dtos.Responses;

public class CommentDto
{
    public Guid Id { get; set; }

    public Guid ResourceId { get; set; }

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;

    public int OwnerId { get; set; }
    
    public UserDto Owner { get; set; } = null!;

    public DateTimeOffset DateCreated { get; set; }

    public int ReplyCount { get; set; }

    public int LikeCount { get; set; }

    public DateTimeOffset? DateLiked { get; set; }
}