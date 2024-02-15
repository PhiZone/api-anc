namespace PhiZoneApi.Dtos.Responses;

public class UserDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public int Gender { get; set; }

    public RegionDto Region { get; set; } = null!;

    public string Language { get; set; } = null!;

    public string? Biography { get; set; }

    public string? Role { get; set; }

    public int Experience { get; set; }

    public string? Tag { get; set; }

    public double Rks { get; set; }

    public int FollowerCount { get; set; }

    public int FolloweeCount { get; set; }

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public DateTimeOffset? DateFollowed { get; set; }

    public List<ApplicationUserDto>? ApplicationLinks { get; set; }
}