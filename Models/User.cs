using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PhiZoneApi.Models;

public class User : IdentityUser<int>
{
    [Url] public string? Avatar { get; set; }

    [Required] [Range(0, 2)] public int Gender { get; set; }

    [MaxLength(2000)] public string? Biography { get; set; }

    public double Experience { get; set; }

    [MaxLength(16)] public string? Tag { get; set; }

    public double Rks { get; set; }

    [Required]
    [RegularExpression(@"^[a-z]{2}(?:-[A-Z]{2})?$")]
    public required string Language { get; set; }

    public DateTimeOffset? DateLastLoggedIn { get; set; }

    public DateTimeOffset? DateLastModifiedUserName { get; set; }

    public DateTimeOffset DateJoined { get; set; }

    [DataType(DataType.Date)] public DateTimeOffset? DateOfBirth { get; set; }

    public Region? Region { get; set; }

    public ICollection<UserRelation> Followers { get; } = new List<UserRelation>();

    public ICollection<UserRelation> Followees { get; } = new List<UserRelation>();
}