using Newtonsoft.Json;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Chart : SignificantResource
{
    public string? Title { get; set; }

    public ChartLevel LevelType { get; set; }

    public string Level { get; set; } = null!;

    public double Difficulty { get; set; }

    public ChartFormat Format { get; set; }

    public string? File { get; set; }

    public string? FileChecksum { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }

    public bool IsRanked { get; set; }

    public int NoteCount { get; set; }

    public double Score { get; set; }

    public double Rating { get; set; } = 2.5;

    public double RatingOnArrangement { get; set; } = 2.5;

    public double RatingOnGameplay { get; set; } = 2.5;

    public double RatingOnVisualEffects { get; set; } = 2.5;

    public double RatingOnCreativity { get; set; } = 2.5;

    public double RatingOnConcord { get; set; } = 2.5;

    public double RatingOnImpression { get; set; } = 2.5;

    public Guid SongId { get; set; }

    public Song Song { get; set; } = null!;

    public long PlayCount { get; set; }

    public List<Tag> Tags { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    [JsonIgnore]
    public List<ChartAsset> Assets { get; } = [];

    public override string GetDisplay()
    {
        return
            $"{(Title != null ? $"{Title} " : Song.Title)}[{Level} {(Difficulty == 0 ? "?" : Math.Floor(Difficulty))}]";
    }
}