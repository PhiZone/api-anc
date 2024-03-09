using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class PhiraUserDto
{
    [JsonProperty("id")] public int Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = null!;

    [JsonProperty("avatar")] public string? Avatar { get; set; }

    [JsonProperty("badges")] public List<string> Badges { get; set; } = null!;

    [JsonProperty("language")] public string Language { get; set; } = null!;

    [JsonProperty("bio")] public string? Bio { get; set; }

    [JsonProperty("exp")] public long Exp { get; set; }

    [JsonProperty("rks")] public float Rks { get; set; }

    [JsonProperty("joined")] public DateTimeOffset Joined { get; set; }

    [JsonProperty("last_login")] public DateTimeOffset LastLogin { get; set; }

    [JsonProperty("roles")] public int Roles { get; set; }

    [JsonProperty("banned")] public bool Banned { get; set; }

    [JsonProperty("follower_count")] public long FollowerCount { get; set; }

    [JsonProperty("following_count")] public long FollowingCount { get; set; }
}