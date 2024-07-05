using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class DiscordUserDto
{
    [JsonProperty("id")] public string Id { get; set; } = null!;

    [JsonProperty("username")] public string Username { get; set; } = null!;

    [JsonProperty("discriminator")] public string Discriminator { get; set; } = null!;

    [JsonProperty("global_name")] public string? GlobalName { get; set; }

    [JsonProperty("avatar")] public string? Avatar { get; set; }

    [JsonProperty("bot")] public bool? Bot { get; set; }

    [JsonProperty("system")] public bool? System { get; set; }

    [JsonProperty("mfa_enabled")] public bool? MfaEnabled { get; set; }

    [JsonProperty("banner")] public string? Banner { get; set; }

    [JsonProperty("accent_color")] public int? AccentColor { get; set; }

    [JsonProperty("locale")] public string? Locale { get; set; }

    [JsonProperty("verified")] public bool? Verified { get; set; }

    [JsonProperty("email")] public string? Email { get; set; }

    [JsonProperty("flags")] public int? Flags { get; set; }

    [JsonProperty("premium_type")] public int? PremiumType { get; set; }

    [JsonProperty("public_flags")] public int? PublicFlags { get; set; }

    [JsonProperty("avatar_decoration")] public string? AvatarDecoration { get; set; }
}