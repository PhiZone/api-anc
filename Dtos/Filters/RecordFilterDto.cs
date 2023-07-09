using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class RecordFilterDto : FilterDto<Record>
{
    public List<Guid>? RangeId { get; set; }
    
    public int? MinPlayerId { get; set; }
    
    public int? MaxPlayerId { get; set; }
    
    public List<int>? RangePlayerId { get; set; }
    
    public List<Guid>? RangeChartId { get; set; }
    
    public int? MinScore { get; set; }
    
    public int? MaxScore { get; set; }
    
    public List<int>? RangeScore { get; set; }
    
    public double? MinAccuracy { get; set; }
    
    public double? MaxAccuracy { get; set; }
    
    public List<double>? RangeAccuracy { get; set; }
    
    public bool? IsFullCombo { get; set; }
    
    public int? MinMaxCombo { get; set; }
    
    public int? MaxCombo { get; set; }
    
    public List<int>? RangeMaxCombo { get; set; }
    
    public int? MinPerfect { get; set; }
    
    public int? MaxPerfect { get; set; }
    
    public List<int>? RangePerfect { get; set; }
    
    public int? MinGoodEarly { get; set; }
    
    public int? MaxGoodEarly { get; set; }
    
    public List<int>? RangeGoodEarly { get; set; }
    
    public int? MinGoodLate { get; set; }
    
    public int? MaxGoodLate { get; set; }
    
    public List<int>? RangeGoodLate { get; set; }
    
    public int? MinBad { get; set; }
    
    public int? MaxBad { get; set; }
    
    public List<int>? RangeBad { get; set; }
    
    public int? MinMiss { get; set; }
    
    public int? MaxMiss { get; set; }
    
    public List<int>? RangeMiss { get; set; }
    
    public double? MinRks { get; set; }
    
    public double? MaxRks { get; set; }
    
    public List<int>? RangeRks { get; set; }
    
    public int? MinPerfectJudgment { get; set; }
    
    public int? MaxPerfectJudgment { get; set; }
    
    public List<int>? RangePerfectJudgment { get; set; }
    
    public int? MinGoodJudgment { get; set; }
    
    public int? MaxGoodJudgment { get; set; }
    
    public List<int>? RangeGoodJudgment { get; set; }
    
    public int? MinAppId { get; set; }
    
    public int? MaxAppId { get; set; }
    
    public List<int>? RangeAppId { get; set; }
    
    public DateTimeOffset? EarliestDateCreated { get; set; }
    
    public DateTimeOffset? LatestDateCreated { get; set; }
}