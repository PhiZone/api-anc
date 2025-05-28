using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public abstract class LikeableResource : OwnedResource
{
    public int LikeCount { get; set; }

    [JsonIgnore] public List<Like> Likes { get; } = [];

    public abstract string GetDisplay();
}