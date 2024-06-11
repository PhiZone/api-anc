using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ServiceRecordFilterDto : FilterDto<ServiceRecord>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeResourceId { get; set; }

    public List<Guid>? RangeServiceId { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public string? ContainsResult { get; set; }

    public string? EqualsResult { get; set; }

    public int? MinOwnerId { get; set; }

    public int? MaxOwnerId { get; set; }

    public List<int>? RangeOwnerId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}