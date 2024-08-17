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

    public List<UserRole>? RangeRole { get; set; }

    public ulong? MinExperience { get; set; }

    public ulong? MaxExperience { get; set; }

    public List<ulong>? RangeExperience { get; set; }

    public string? ContainsTag { get; set; }

    public string? EqualsTag { get; set; }

    public List<string>? /* supposed to be nullable */ RangeTag { get; set; }

    public string? ContainsLanguage { get; set; }

    public string? EqualsLanguage { get; set; }

    public List<string>? RangeLanguage { get; set; }

    public double? MinRks { get; set; }

    public double? MaxRks { get; set; }

    public int? MinFollowerCount { get; set; }

    public int? MaxFollowerCount { get; set; }

    public List<int>? RangeFollowerCount { get; set; }

    public int? MinFolloweeCount { get; set; }

    public int? MaxFolloweeCount { get; set; }

    public List<int>? RangeFolloweeCount { get; set; }

    public List<int>? RangeRegionId { get; set; }

    public DateTimeOffset? EarliestDateLastLoggedIn { get; set; }

    public DateTimeOffset? LatestDateLastLoggedIn { get; set; }

    public DateTimeOffset? EarliestDateJoined { get; set; }

    public DateTimeOffset? LatestDateJoined { get; set; }

    public DateTimeOffset? EarliestDateOfBirth { get; set; }

    public DateTimeOffset? LatestDateOfBirth { get; set; }
}