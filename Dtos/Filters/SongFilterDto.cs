using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class SongFilterDto : PublicResourceFilterDto<Song>
{
    public List<EditionType>? RangeEditionType { get; set; }
    
    public string? ContainsEdition { get; set; }
    
    public string? EqualsEdition { get; set; }

    public string? ContainsAuthorName { get; set; }

    public string? EqualsAuthorName { get; set; }
    
    public string? ContainsLyrics { get; set; }
    
    public string? EqualsLyrics { get; set; }
    
    public int? MinBpm { get; set; }
    
    public int? MaxBpm { get; set; }
    
    public List<int>? RangeBpm { get; set; }
    
    public int? MinMinBpm { get; set; }
    
    public int? MaxMinBpm { get; set; }
    
    public List<int>? RangeMinBpm { get; set; }
    
    public int? MinMaxBpm { get; set; }
    
    public int? MaxMaxBpm { get; set; }
    
    public List<int>? RangeMaxBpm { get; set; }
    
    public int? MinOffset { get; set; }
    
    public int? MaxOffset { get; set; }
    
    public List<int>? RangeOffset { get; set; }
    
    public bool? IsOriginal { get; set; }
    
    public TimeSpan? MinDuration { get; set; }
    
    public TimeSpan? MaxDuration { get; set; }
    
    public TimeSpan? MinPreviewStart { get; set; }
    
    public TimeSpan? MaxPreviewStart { get; set; }
    
    public TimeSpan? MinPreviewEnd { get; set; }
    
    public TimeSpan? MaxPreviewEnd { get; set; }
}