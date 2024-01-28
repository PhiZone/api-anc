using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public abstract class LikeableResource : Resource
{
    public int LikeCount { get; set; }
    
    [JsonIgnore] public IEnumerable<Like> Likes { get; } = new List<Like>();

    public abstract string GetDisplay();
}