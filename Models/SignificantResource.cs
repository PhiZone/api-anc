using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public abstract class SignificantResource : PublicResource
{
    public DateTimeOffset DateFileUpdated { get; set; }
    
    [JsonIgnore] public List<EventResource> EventPresences { get; } = [];
}