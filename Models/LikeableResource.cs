namespace PhiZoneApi.Models;

public abstract class LikeableResource : Resource
{
    public int LikeCount { get; set; }

    public abstract string GetDisplay();
}