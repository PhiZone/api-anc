using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class PetAnswerFilterDto : FilterDto<PetAnswer>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeQuestion1 { get; set; }

    public string? ContainsAnswer1 { get; set; }
    
    public string? EqualsAnswer1 { get; set; }

    public List<Guid>? RangeQuestion2 { get; set; }

    public string? ContainsAnswer2 { get; set; }
    
    public string? EqualsAnswer2 { get; set; }

    public List<Guid>? RangeQuestion3 { get; set; }

    public string? ContainsAnswer3 { get; set; }
    
    public string? EqualsAnswer3 { get; set; }

    public string? ContainsChart { get; set; }
    
    public string? EqualsChart { get; set; }

    public int? MinObjectiveScore { get; set; }

    public int? MaxObjectiveScore { get; set; }

    public List<int>? RangeObjectiveScore { get; set; }

    public int? MinSubjectiveScore { get; set; }

    public int? MaxSubjectiveScore { get; set; }

    public List<int>? RangeSubjectiveScore { get; set; }

    public int? MinTotalScore { get; set; }

    public int? MaxTotalScore { get; set; }

    public List<int>? RangeTotalScore { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public int? MinAssessorId { get; set; }

    public int? MaxAssessorId { get; set; }

    public List<int>? RangeAssessorId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public DateTimeOffset? EarliestDateUpdated { get; set; }

    public DateTimeOffset? LatestDateUpdated { get; set; }
}