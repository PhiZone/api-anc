using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public class Tag
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string NormalizedName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTimeOffset DateCreated { get; set; }

    [JsonIgnore] public List<Song> Songs { get; } = [];

    [JsonIgnore] public List<Chart> Charts { get; } = [];
}