using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using System.Text.Json.Serialization;

namespace PhiZoneApi.Models;

public class User : IdentityUser<int>
{
    public string? Avatar { get; set; }

    public Gender Gender { get; set; }

    public string? Biography { get; set; }

    public UserRole Role { get; set; }

    public int Experience { get; set; }

    public string? Tag { get; set; }

    public double Rks { get; set; }

    public string Language { get; set; } = null!;

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset? DateLastModifiedUserName { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    public DateTimeOffset? DateOfBirth { get; set; }

    public int RegionId { get; set; }

    public Region Region { get; set; } = null!;
    
    [JsonIgnore]
    public IEnumerable<User> Followers { get; } = new List<User>();

    [JsonIgnore]
    public IEnumerable<UserRelation> FollowerRelations { get; } = new List<UserRelation>();

    [JsonIgnore]
    public IEnumerable<User> Followees { get; } = new List<User>();

    [JsonIgnore]
    public IEnumerable<UserRelation> FolloweeRelations { get; } = new List<UserRelation>();

    [JsonIgnore]
    public IEnumerable<Application> TapApplications { get; } = new List<Application>();

    [JsonIgnore]
    public IEnumerable<TapUserRelation> TapUserRelations { get; } = new List<TapUserRelation>();

    [JsonIgnore]
    public IEnumerable<EventTeam> EventTeams { get; } = new List<EventTeam>();

    [JsonIgnore]
    public IEnumerable<Participation> Participations { get; } = new List<Participation>();
}