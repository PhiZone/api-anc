using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public abstract class SignificantResource : PublicResource
{
    [JsonIgnore] public List<EventResource> EventPresences { get; } = [];
}