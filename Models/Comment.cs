namespace PhiZoneApi.Models;

public class Comment : LikeableResource
{
    public Guid ResourceId { get; set; }

    public LikeableResource Resource { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Language { get; set; } = null!;

    public override string GetDisplay()
    {
        return Content;
    }
}