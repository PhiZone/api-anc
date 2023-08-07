using Microsoft.AspNetCore.Identity;

namespace PhiZoneApi.Models;

public class User : IdentityUser<int>
{
    public string? Avatar { get; set; }

    public int Gender { get; set; }

    public string? Biography { get; set; }

    public int Experience { get; set; }

    public string? Tag { get; set; }

    public double Rks { get; set; }

    public required string Language { get; set; }

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset? DateLastModifiedUserName { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public int? RegionId { get; set; }

    public Region? Region { get; set; }

    public IEnumerable<User> Followers { get; } = new List<User>();

    public IEnumerable<UserRelation> FollowerRelations { get; } = new List<UserRelation>();

    public IEnumerable<User> Followees { get; } = new List<User>();

    public IEnumerable<UserRelation> FolloweeRelations { get; } = new List<UserRelation>();
}