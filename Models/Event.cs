using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public class Event : PublicResource
{
    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public DateTimeOffset DateUnveiled { get; set; }

    [JsonIgnore] public List<EventDivision> Divisions { get; set; } = [];

    [JsonIgnore] public List<User> Hosts { get; set; } = [];

    [JsonIgnore] public List<Hostship> Hostships { get; set; } = [];

    public override string GetDisplay()
    {
        return Title;
    }
}