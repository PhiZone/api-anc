namespace PhiZoneApi.Models;

public class PetAnswer
{
    public Guid Id { get; set; }
    
    public Guid Question1 { get; set; }
    
    public string Answer1 { get; set; } = null!;
    
    public Guid Question2 { get; set; }
    
    public string Answer2 { get; set; } = null!;
    
    public Guid Question3 { get; set; }
    
    public string Answer3 { get; set; } = null!;

    public string Chart { get; set; } = null!;
    
    public int ObjectiveScore { get; set; }
    
    public int? SubjectiveScore { get; set; }
    
    public int? TotalScore { get; set; }
    
    public int OwnerId { get; set; }
    
    public User Owner { get; set; } = null!;
    
    public int? AssessorId { get; set; }
    
    public User? Assessor { get; set; }
    
    public DateTimeOffset DateCreated { get; set; }
    
    public DateTimeOffset DateUpdated { get; set; }
}