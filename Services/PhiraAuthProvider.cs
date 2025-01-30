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

public class PhiraAuthProvider : IAuthProvider
{
    private readonly Guid _applicationId;
    private readonly string _avatarUrl;
    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _illustrationUrl;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;

    public PhiraAuthProvider(IOptions<List<AuthProvider>> authProviders, IConnectionMultiplexer redis,
        IServiceProvider serviceProvider)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        var providerSettings = authProviders.Value.First(e => e.Name == "Phira");
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
                Name = "Phira",
                Avatar = _avatarUrl,
                Illustration = _illustrationUrl,
                Illustrator = "Phira",
                Homepage = "https://phira.moe",
                Type = ApplicationType.MobilePlayer,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await applicationRepository.CreateApplicationAsync(application);
        }
    }

    public async Task<ServiceResponseDto> RequestIdentityAsync(string state, string redirectUri, User? user = null)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync($"phizone:phira:{state}", user?.Id.ToString() ?? string.Empty, TimeSpan.FromHours(2));
        var query = new QueryBuilder
        {
            { "clientID", _clientId },
            { "state", state },
            { "scope", "1" },
            { "redirectURI", $"https://redirect.phi.zone/?uri={redirectUri}" }
        };
        if (user != null) query.Add("login", user.Email!);

        return new ServiceResponseDto
        {
            Type = ServiceResponseType.Redirect,
            RedirectUri = new UriBuilder("https://phira.moe/oauth") { Query = query.ToString() }.Uri.ToString(),
            Message = null
        };
    }

    public async Task<(string, string?)?> RequestTokenAsync(string code, string state, User? user = null,
        string? redirectUri = null)
    {
        var db = _redis.GetDatabase();
        var key = $"phizone:phira:{state}";
        if (!await db.KeyExistsAsync(key)) return null;

        var userId = await db.StringGetAsync(key);
        await db.KeyDeleteAsync(key);
        if ((user == null && userId != string.Empty) || (user != null && userId != user.Id.ToString())) return null;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new UriBuilder("https://api.phira.cn/oauth/token")
            {
                Query = new QueryBuilder
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "code", code }
                }.ToString()
            }.Uri,
            Headers = { { "Accept", "application/json" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<PhiraTokenDto>(await response.Content.ReadAsStringAsync())!;
        // ReSharper disable once InvertIf
        if (user != null) await UpdateTokenAsync(user, content.AccessToken, content.RefreshToken);

        return new ValueTuple<string, string?>(content.AccessToken, content.RefreshToken);
    }

    public async Task<User?> GetIdentityAsync(string accessToken)
    {
        var response = await RetrieveIdentityAsync(accessToken);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<PhiraUserDto>(await response.Content.ReadAsStringAsync())!;
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return await userRepository.GetUserByRemoteIdAsync(_applicationId, content.Id.ToString());
    }

    public async Task<RemoteUserDto?> GetRemoteIdentityAsync(string accessToken)
    {
        var response = await RetrieveIdentityAsync(accessToken);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<PhiraUserDto>(await response.Content.ReadAsStringAsync())!;

        return new RemoteUserDto
        {
            Id = content.Id.ToString(),
            UserName = content.Name,
            Email = content.Email,
            Avatar = content.Avatar != null
                ? await _client.GetByteArrayAsync(content.Avatar
                    // .Replace("api.phira.cn/files/",
                    // "files-cf.phira.cn/")
                )
                : null
        };
    }

    public async Task<bool> BindIdentityAsync(User user)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        if (!await applicationUserRepository.RelationExistsAsync(_applicationId, user.Id)) return false;

        var applicationUser = await applicationUserRepository.GetRelationAsync(_applicationId, user.Id);
        if (applicationUser.AccessToken == null) return false;

        var response = await RetrieveIdentityAsync(applicationUser.AccessToken);
        if (!response.IsSuccessStatusCode) return false;

        var content = JsonConvert.DeserializeObject<PhiraUserDto>(await response.Content.ReadAsStringAsync())!;
        var existingUser = await userRepository.GetUserByRemoteIdAsync(_applicationId, content.Id.ToString());
        if (existingUser != null && existingUser.Id != user.Id) return false;

        applicationUser.RemoteUserId = content.Id.ToString();
        applicationUser.RemoteUserName = content.Name;
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
            applicationUser.RefreshToken = refreshToken;
            applicationUser.DateUpdated = DateTimeOffset.UtcNow;
            await applicationUserRepository.UpdateRelationAsync(applicationUser);
        }
    }

    public async Task<bool> RefreshTokenAsync(User user)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var applicationUserRepository = scope.ServiceProvider.GetRequiredService<IApplicationUserRepository>();
        if (!await applicationUserRepository.RelationExistsAsync(_applicationId, user.Id)) return false;
        var applicationUser = await applicationUserRepository.GetRelationAsync(_applicationId, user.Id);
        if (applicationUser.RefreshToken == null) return false;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new UriBuilder("https://api.phira.cn/oauth/token")
            {
                Query = new QueryBuilder
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "refresh_token", applicationUser.RefreshToken }
                }.ToString()
            }.Uri,
            Headers = { { "Accept", "application/json" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        var content = JsonConvert.DeserializeObject<PhiraTokenDto>(await response.Content.ReadAsStringAsync())!;
        await UpdateTokenAsync(user, content.AccessToken, content.RefreshToken);
        return true;
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
            RequestUri = new Uri("https://api.phira.cn/me"),
            Headers = { { "Authorization", $"Bearer {accessToken}" } }
        };
        return await _client.SendAsync(request);
    }
}