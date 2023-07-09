namespace PhiZoneApi.Models;

public class Comment : LikeableResource
{
    public Guid ResourceId { get; set; }

    public PublicResource Resource { get; set; } = null!;
    
    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;
}