using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class User : IdentityUser<int>, IEquatable<User>
{
    public string? Avatar { get; set; }

    public Gender Gender { get; set; }

    public string? Biography { get; set; }

    public int Experience { get; set; }

    public string? Tag { get; set; }

    public double Rks { get; set; }

    public string Language { get; set; } = null!;

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset? DateLastModifiedUserName { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    public string? TapUnionId { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public int RegionId { get; set; }

    public Region Region { get; set; } = null!;

    public IEnumerable<User> Followers { get; } = new List<User>();

    public IEnumerable<UserRelation> FollowerRelations { get; } = new List<UserRelation>();

    public IEnumerable<User> Followees { get; } = new List<User>();

    public IEnumerable<UserRelation> FolloweeRelations { get; } = new List<UserRelation>();

    public bool Equals(User? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }
}