using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public class Tag : Resource
{
    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string? Description { get; set; }

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public List<Song> Songs { get; } = [];

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public List<Chart> Charts { get; } = [];

    [JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public List<EventResource> EventPresences { get; } = [];
}