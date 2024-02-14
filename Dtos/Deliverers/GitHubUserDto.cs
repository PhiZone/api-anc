using Newtonsoft.Json;

namespace PhiZoneApi.Dtos.Deliverers;

public class GitHubUserDto
{
    [JsonProperty("avatar_url", Required = Required.Always)]
    public Uri AvatarUrl { get; set; } = null!;

    [JsonProperty("bio", Required = Required.AllowNull)]
    public string Bio { get; set; } = null!;

    [JsonProperty("blog", Required = Required.AllowNull)]
    public string Blog { get; set; } = null!;

    [JsonProperty("business_plus", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public bool? BusinessPlus { get; set; }

    [JsonProperty("collaborators", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? Collaborators { get; set; }

    [JsonProperty("company", Required = Required.AllowNull)]
    public string Company { get; set; } = null!;

    [JsonProperty("created_at", Required = Required.Always)]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonProperty("disk_usage", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? DiskUsage { get; set; }

    [JsonProperty("email", Required = Required.AllowNull)]
    public string Email { get; set; } = null!;

    [JsonProperty("events_url", Required = Required.Always)]
    public string EventsUrl { get; set; } = null!;

    [JsonProperty("followers", Required = Required.Always)]
    public long Followers { get; set; }

    [JsonProperty("followers_url", Required = Required.Always)]
    public Uri FollowersUrl { get; set; } = null!;

    [JsonProperty("following", Required = Required.Always)]
    public long Following { get; set; }

    [JsonProperty("following_url", Required = Required.Always)]
    public string FollowingUrl { get; set; } = null!;

    [JsonProperty("gists_url", Required = Required.Always)]
    public string GistsUrl { get; set; } = null!;

    [JsonProperty("gravatar_id", Required = Required.AllowNull)]
    public string GravatarId { get; set; } = null!;

    [JsonProperty("hireable", Required = Required.AllowNull)]
    public bool? Hireable { get; set; }

    [JsonProperty("html_url", Required = Required.Always)]
    public Uri HtmlUrl { get; set; } = null!;

    [JsonProperty("id", Required = Required.Always)]
    public long Id { get; set; }

    [JsonProperty("ldap_dn", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public string LdapDn { get; set; } = null!;

    [JsonProperty("location", Required = Required.AllowNull)]
    public string Location { get; set; } = null!;

    [JsonProperty("login", Required = Required.Always)]
    public string Login { get; set; } = null!;

    [JsonProperty("name", Required = Required.AllowNull)]
    public string Name { get; set; } = null!;

    [JsonProperty("node_id", Required = Required.Always)]
    public string NodeId { get; set; } = null!;

    [JsonProperty("organizations_url", Required = Required.Always)]
    public Uri OrganizationsUrl { get; set; } = null!;

    [JsonProperty("owned_private_repos", Required = Required.DisallowNull,
        NullValueHandling = NullValueHandling.Ignore)]
    public long? OwnedPrivateRepos { get; set; }

    [JsonProperty("plan", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public Plan Plan { get; set; } = null!;

    [JsonProperty("private_gists", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? PrivateGists { get; set; }

    [JsonProperty("public_gists", Required = Required.Always)]
    public long PublicGists { get; set; }

    [JsonProperty("public_repos", Required = Required.Always)]
    public long PublicRepos { get; set; }

    [JsonProperty("received_events_url", Required = Required.Always)]
    public Uri ReceivedEventsUrl { get; set; } = null!;

    [JsonProperty("repos_url", Required = Required.Always)]
    public Uri ReposUrl { get; set; } = null!;

    [JsonProperty("site_admin", Required = Required.Always)]
    public bool SiteAdmin { get; set; }

    [JsonProperty("starred_url", Required = Required.Always)]
    public string StarredUrl { get; set; } = null!;

    [JsonProperty("subscriptions_url", Required = Required.Always)]
    public Uri SubscriptionsUrl { get; set; } = null!;

    [JsonProperty("suspended_at")] public DateTimeOffset? SuspendedAt { get; set; }

    [JsonProperty("total_private_repos", Required = Required.DisallowNull,
        NullValueHandling = NullValueHandling.Ignore)]
    public long? TotalPrivateRepos { get; set; }

    [JsonProperty("two_factor_authentication", Required = Required.DisallowNull,
        NullValueHandling = NullValueHandling.Ignore)]
    public bool? TwoFactorAuthentication { get; set; }

    [JsonProperty("type", Required = Required.Always)]
    public string Type { get; set; } = null!;

    [JsonProperty("updated_at", Required = Required.Always)]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonProperty("url", Required = Required.Always)]
    public Uri Url { get; set; } = null!;
}

public class Plan
{
    [JsonProperty("collaborators", Required = Required.Always)]
    public long Collaborators { get; set; }

    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; } = null!;

    [JsonProperty("private_repos", Required = Required.Always)]
    public long PrivateRepos { get; set; }

    [JsonProperty("space", Required = Required.Always)]
    public long Space { get; set; }
}