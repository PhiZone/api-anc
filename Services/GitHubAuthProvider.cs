using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;
using AuthProvider = PhiZoneApi.Configurations.AuthProvider;

namespace PhiZoneApi.Services;

public class GitHubAuthProvider : IAuthProvider
{
    private readonly Guid _applicationId;
    private readonly string _avatarUrl;
    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _illustrationUrl;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;

    public GitHubAuthProvider(IOptions<List<AuthProvider>> authProviders, IConnectionMultiplexer redis,
        IServiceProvider serviceProvider)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        var providerSettings = authProviders.Value.First(e => e.Name == "GitHub");
        _clientId = providerSettings.ClientId;
        _clientSecret = providerSettings.ClientSecret;
        _applicationId = providerSettings.ApplicationId;
        _avatarUrl = providerSettings.AvatarUrl;
        _illustrationUrl = providerSettings.IllustrationUrl;
    }

    public async Task InitializeAsync()
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationRepository = scope.ServiceProvider.GetRequiredService<IApplicationRepository>();
        if (!await applicationRepository.ApplicationExistsAsync(_applicationId))
        {
            var application = new Application
            {
                Id = _applicationId,
                OwnerId = 1,
                Name = "GitHub",
                Avatar = _avatarUrl,
                Illustration = _illustrationUrl,
                Illustrator = "GitHub",
                Homepage = "https://github.com",
                Type = ApplicationType.AuthProvider,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await applicationRepository.CreateApplicationAsync(application);
        }
    }

    public async Task<ServiceResponseDto> RequestIdentityAsync(string state, string redirectUri, User? user = null)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"phizone:github:{state}", user?.Id.ToString() ?? string.Empty, TimeSpan.FromHours(2));
        var query = new QueryBuilder
        {
            { "client_id", _clientId }, { "state", state },
            { "redirect_uri", $"https://redirect.phi.zone/?uri={redirectUri}" }
        };
        if (user != null) query.Add("login", user.Email!);

        return new ServiceResponseDto
        {
            Type = ServiceResponseType.Redirect,
            RedirectUri =
                new UriBuilder("https://github.com/login/oauth/authorize") { Query = query.ToString() }.Uri,
            Message = null
        };
    }

    public async Task<(string, string?)?> RequestTokenAsync(string code, string state, User? user = null)
    {
        var db = _redis.GetDatabase();
        var key = $"phizone:github:{state}";
        if (!await db.KeyExistsAsync(key)) return null;

        var userId = await db.StringGetAsync(key);
        await db.KeyDeleteAsync(key);
        if ((user == null && userId != string.Empty) || (user != null && userId != user.Id.ToString())) return null;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new UriBuilder("https://github.com/login/oauth/access_token")
            {
                Query = new QueryBuilder
                {
                    { "client_id", _clientId }, { "client_secret", _clientSecret }, { "code", code }
                }.ToString()
            }.Uri,
            Headers = { { "Accept", "application/json" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<GitHubTokenDto>(await response.Content.ReadAsStringAsync())!;
        // ReSharper disable once InvertIf
        if (user != null) await UpdateTokenAsync(user, content.AccessToken);

        return new ValueTuple<string, string?>(content.AccessToken, null);
    }

    public async Task<User?> GetIdentityAsync(string accessToken)
    {
        var response = await RetrieveIdentityAsync(accessToken);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<GitHubUserDto>(await response.Content.ReadAsStringAsync())!;
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return await userRepository.GetUserByRemoteIdAsync(_applicationId, content.Login);
    }

    public async Task<bool> BindIdentityAsync(User user)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (!await applicationUserRepository.RelationExistsAsync(_applicationId, user.Id))
        {
            return false;
        }

        var applicationUser = await applicationUserRepository.GetRelationAsync(_applicationId, user.Id);
        if (applicationUser.AccessToken == null)
        {
            return false;
        }

        var response = await RetrieveIdentityAsync(applicationUser.AccessToken);
        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var content = JsonConvert.DeserializeObject<GitHubUserDto>(await response.Content.ReadAsStringAsync())!;
        var existingUser = await userRepository.GetUserByRemoteIdAsync(_applicationId, content.Login);
        if (existingUser != null && existingUser.Id != user.Id)
        {
            return false;
        }

        applicationUser.RemoteUserId = content.Login;
        await applicationUserRepository.UpdateRelationAsync(applicationUser);
        return true;
    }

    public async Task UpdateTokenAsync(User user, string accessToken, string? refreshToken = null)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        if (!await applicationUserRepository.RelationExistsAsync(_applicationId, user.Id))
        {
            var applicationUser = new ApplicationUser
            {
                ApplicationId = _applicationId,
                UserId = user.Id,
                AccessToken = accessToken,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await applicationUserRepository.CreateRelationAsync(applicationUser);
        }
        else
        {
            var applicationUser = await applicationUserRepository.GetRelationAsync(_applicationId, user.Id);
            applicationUser.AccessToken = accessToken;
            applicationUser.DateUpdated = DateTimeOffset.UtcNow;
            await applicationUserRepository.UpdateRelationAsync(applicationUser);
        }
    }

    public async Task RevokeTokenAsync(User user)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        if (await applicationUserRepository.RelationExistsAsync(_applicationId, user.Id))
        {
            var applicationUser = await applicationUserRepository.GetRelationAsync(_applicationId, user.Id);
            applicationUser.AccessToken = null;
            applicationUser.RefreshToken = null;
            applicationUser.DateAccessTokenExpires = null;
            applicationUser.DateRefreshTokenExpires = null;
            applicationUser.DateUpdated = DateTimeOffset.UtcNow;
            await applicationUserRepository.UpdateRelationAsync(applicationUser);
        }
    }

    public Guid GetApplicationId()
    {
        return _applicationId;
    }

    private async Task<HttpResponseMessage> RetrieveIdentityAsync(string accessToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("https://api.github.com/user"),
            Headers = { { "Authorization", $"Bearer {accessToken}" }, { "User-Agent", "PhiZone" } }
        };
        return await _client.SendAsync(request);
    }
}