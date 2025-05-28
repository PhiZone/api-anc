using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class ResourceRecordFilterDto : FilterDto<ResourceRecord>
{
    public List<Guid>? RangeId { get; set; }

    public List<ResourceRecordType>? RangeType { get; set; }

    public string? ContainsTitle { get; set; }

    public string? EqualsTitle { get; set; }

    public List<EditionType>? RangeEditionType { get; set; }

    public string? ContainsEdition { get; set; }

    public string? EqualsEdition { get; set; }

    public string? ContainsAuthorName { get; set; }

    public string? EqualsAuthorName { get; set; }

    public string? ContainsDescription { get; set; }

    public string? EqualsDescription { get; set; }

    public List<ResourceRecordStrategy>? RangeStrategy { get; set; }

    public string? ContainsSource { get; set; }

    public string? EqualsSource { get; set; }

    public string? ContainsCopyrightOwner { get; set; }

    public string? EqualsCopyrightOwner { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }

    public DateTimeOffset? EarliestDateUpdated { get; set; }

    public DateTimeOffset? LatestDateUpdated { get; set; }
}