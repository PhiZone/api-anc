using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;

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

    public int FollowerCount { get; set; }

    public int FolloweeCount { get; set; }

    public int RegionId { get; set; }

    public Region Region { get; set; } = null!;

    [JsonIgnore] public List<User> Followers { get; } = [];

    [JsonIgnore] public List<UserRelation> FollowerRelations { get; } = [];

    [JsonIgnore] public List<User> Followees { get; } = [];

    [JsonIgnore] public List<UserRelation> FolloweeRelations { get; } = [];

    [JsonIgnore] public List<Application> Applications { get; } = [];

    [JsonIgnore] public List<ApplicationUser> ApplicationLinks { get; } = [];

    [JsonIgnore] public List<EventTeam> EventTeams { get; } = [];

    [JsonIgnore] public List<Participation> Participations { get; } = [];
}