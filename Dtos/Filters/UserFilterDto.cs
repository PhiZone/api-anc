using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Dtos.Filters;

public class UserFilterDto : FilterDto<User>
{
    public int? MinId { get; set; }

    public int? MaxId { get; set; }

    public List<int>? RangeId { get; set; }

    public string? ContainsUserName { get; set; }

    public string? EqualsUserName { get; set; }

    public List<Gender>? RangeGender { get; set; }

    public int? MinExperience { get; set; }

    public int? MaxExperience { get; set; }

    public string? ContainsTag { get; set; }

    public string? EqualsTag { get; set; }

    public List<string>? RangeTag { get; set; }

    public string? ContainsLanguage { get; set; }

    public string? EqualsLanguage { get; set; }

    public List<string>? RangeLanguage { get; set; }

    public double? MinRks { get; set; }

    public double? MaxRks { get; set; }

    public List<int>? RangeRegionId { get; set; }

    public DateTimeOffset? EarliestDateLastLoggedIn { get; set; }

    public DateTimeOffset? LatestDateLastLoggedIn { get; set; }

    public DateTimeOffset? EarliestDateJoined { get; set; }

    public DateTimeOffset? LatestDateJoined { get; set; }

    public DateTimeOffset? EarliestDateOfBirth { get; set; }

    public DateTimeOffset? LatestDateOfBirth { get; set; }
}