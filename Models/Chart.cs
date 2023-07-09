using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Chart : PublicResource
{
    public string? Title { get; set; }
    public ChartLevel LevelType { get; set; }

    public string Level { get; set; } = null!;
    
    public double Difficulty { get; set; }
    
    public ChartFormat Format { get; set; }
    
    public string? File { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Illustration { get; set; }

    public string? Illustrator { get; set; }
    
    public bool IsRanked { get; set; }
    
    public int NoteCount { get; set; }
    
    public double Score { get; set; }
    
    public double Rating { get; set; }
    
    public double RatingOnArrangement { get; set; }
    
    public double RatingOnFeel { get; set; }
    
    public double RatingOnVisualEffects { get; set; }
    
    public double RatingOnCreativity { get; set; }
    
    public double RatingOnConcord { get; set; }
    
    public double RatingOnImpression { get; set; }
    
    public Guid SongId { get; set; }
    
    public Song Song { get; set; } = null!;

    public IEnumerable<User> Authors { get; } = new List<User>();
    
    public int PlayCount { get; set; }
}