using System.Text.Json.Serialization;
using PhiZoneApi.Enums;

namespace PhiZoneApi.Models;

public class Application : LikeableResource
{
    public string Name { get; set; } = null!;

    public string Avatar { get; set; } = null!;

    public string Illustration { get; set; } = null!;

    public string Illustrator { get; set; } = null!;

    public string? Description { get; set; }

    public string Homepage { get; set; } = null!;

    public string? ApiEndpoint { get; set; }

    public ApplicationType Type { get; set; }

    public string? TapClientId { get; set; }

    public string? Secret { get; set; }

    public DateTimeOffset DateUpdated { get; set; }

    [JsonIgnore] public List<User> ApplicationUsers { get; } = [];

    [JsonIgnore] public List<ApplicationUser> ApplicationUserRelations { get; } = [];

    public override string GetDisplay()
    {
        return $"{Name}";
    }
}