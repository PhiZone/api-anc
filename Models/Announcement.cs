namespace PhiZoneApi.Models;

public class Announcement : LikeableResource
{
    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTimeOffset DateUpdated { get; set; }
    
    public override string GetDisplay()
    {
        return $"{Title}";
    }
}