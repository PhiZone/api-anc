using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class AuthorshipFilterDto : FilterDto<Authorship>
{
    public List<Guid>? RangeId { get; set; }

    public List<Guid>? RangeResourceId { get; set; }

    public int? MinAuthorId { get; set; }

    public int? MaxAuthorId { get; set; }

    public List<int>? RangeAuthorId { get; set; }

    public string? ContainsPosition { get; set; }

    public string? EqualsPosition { get; set; }

    public List<string>? /* supposed to be nullable */ RangePosition { get; set; }

    public DateTimeOffset? EarliestDateCreated { get; set; }

    public DateTimeOffset? LatestDateCreated { get; set; }
}