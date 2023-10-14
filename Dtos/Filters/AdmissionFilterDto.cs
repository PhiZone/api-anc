using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class AdmissionFilterDto : FilterDto<Admission>
{
    public List<Guid>? RangeAdmitterId { get; set; }

    public List<Guid>? RangeAdmitteeId { get; set; }

    public List<RequestStatus>? RangeStatus { get; set; }

    public string? ContainsLabel { get; set; }

    public string? EqualsLabel { get; set; }

    public int? MinRequesterId { get; set; }

    public int? MaxRequesterId { get; set; }

    public List<int>? RangeRequesterId { get; set; }

    public int? MinRequesteeId { get; set; }

    public int? MaxRequesteeId { get; set; }

    public List<int>? RangeRequesteeId { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}