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

public class QqAuthProvider : IAuthProvider
{
    private readonly Guid _applicationId;
    private readonly string _avatarUrl;
    private readonly HttpClient _client = new();
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _illustrationUrl;
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;

    public QqAuthProvider(IOptions<List<AuthProvider>> authProviders, IConnectionMultiplexer redis,
        IServiceProvider serviceProvider)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;
        var providerSettings = authProviders.Value.First(e => e.Name == "QQ");
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
                Name = "QQ",
                Avatar = _avatarUrl,
                Illustration = _illustrationUrl,
                Illustrator = "QQ",
                Homepage = "https://im.qq.com",
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
        await db.StringSetAsync($"phizone:qq:{state}", user?.Id.ToString() ?? string.Empty, TimeSpan.FromHours(2));
        var query = new QueryBuilder
        {
            { "response_type", "code" },
            { "client_id", _clientId },
            { "state", state },
            { "scope", "get_user_info" },
            { "redirect_uri", $"https://www.phizone.cn/session/redirect/{redirectUri}" }
        };
        if (user != null) query.Add("login", user.Email!);

        return new ServiceResponseDto
        {
            Type = ServiceResponseType.Redirect,
            RedirectUri =
                new UriBuilder("https://graph.qq.com/oauth2.0/authorize") { Query = query.ToString() }.Uri
                    .ToString(),
            Message = null
        };
    }

    public async Task<(string, string?)?> RequestTokenAsync(string code, string state, User? user = null,
        string? redirectUri = null)
    {
        var db = _redis.GetDatabase();
        var key = $"phizone:qq:{state}";
        Console.WriteLine(key);
        if (!await db.KeyExistsAsync(key)) return null;

        var userId = await db.StringGetAsync(key);
        await db.KeyDeleteAsync(key);
        Console.WriteLine(userId);
        Console.WriteLine((user == null && userId != string.Empty) || (user != null && userId != user.Id.ToString()));
        if ((user == null && userId != string.Empty) || (user != null && userId != user.Id.ToString())) return null;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new UriBuilder("https://graph.qq.com/oauth2.0/token")
            {
                Query = new QueryBuilder
                {
                    { "grant_type", "authorization_code" },
                    { "client_id", _clientId },
                    { "client_secret", _clientSecret },
                    { "code", code },
                    { "redirect_uri", $"https://www.phizone.cn/session/redirect/{redirectUri}" },
                    { "fmt", "json" }
                }.ToString()
            }.Uri,
            Headers = { { "Accept", "application/json" } }
        };
        var response = await _client.SendAsync(request);
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<QqTokenDto>(await response.Content.ReadAsStringAsync())!;
        // ReSharper disable once InvertIf
        if (user != null) await UpdateTokenAsync(user, content.AccessToken, content.RefreshToken);

        return new ValueTuple<string, string?>(content.AccessToken, content.RefreshToken);
    }

    public async Task<User?> GetIdentityAsync(string accessToken)
    {
        var id = await GetOpenIdAsync(accessToken);
        if (id == null) return null;
        await using var scope = _serviceProvider.CreateAsyncScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        return await userRepository.GetUserByRemoteIdAsync(_applicationId, id);
    }

    public async Task<RemoteUserDto?> GetRemoteIdentityAsync(string accessToken)
    {
        var id = await GetOpenIdAsync(accessToken);
        if (id == null) return null;
        var response = await RetrieveIdentityAsync(accessToken, id);
        if (!response.IsSuccessStatusCode) return null;

        var content = JsonConvert.DeserializeObject<QqUserDto>(await response.Content.ReadAsStringAsync())!;
        Console.WriteLine(content.Nickname);
        return new RemoteUserDto
        {
            Id = id,
            UserName = content.Nickname,
            Email = $"{id}@qq.phizone.cn",
            Avatar = !string.IsNullOrEmpty(content.FigureUrlQq2)
                ? await _client.GetByteArrayAsync(content.FigureUrlQq2)
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
        Console.WriteLine(applicationUser.AccessToken);
        var id = await GetOpenIdAsync(applicationUser.AccessToken);
        if (id == null) return false;
        var response = await RetrieveIdentityAsync(applicationUser.AccessToken, id);
        if (!response.IsSuccessStatusCode) return false;
        
        var content = JsonConvert.DeserializeObject<QqUserDto>(await response.Content.ReadAsStringAsync())!;
        var existingUser = await userRepository.GetUserByRemoteIdAsync(_applicationId, id);
        if (existingUser != null && existingUser.Id != user.Id) return false;
        Console.WriteLine(content.Nickname);
        applicationUser.RemoteUserId = id;
        applicationUser.RemoteUserName = content.Nickname;
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
            RequestUri = new UriBuilder("https://api.qq.cn/oauth/token")
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

        var content = JsonConvert.DeserializeObject<QqTokenDto>(await response.Content.ReadAsStringAsync())!;
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

    private async Task<HttpResponseMessage> RetrieveIdentityAsync(string accessToken, string openId)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder("https://graph.qq.com/user/get_user_info")
            {
                Query = new QueryBuilder
                {
                    { "access_token", accessToken },
                    { "oauth_consumer_key", _clientId },
                    { "openid", openId },
                    { "fmt", "json" }
                }.ToString()
            }.Uri,
            Headers = { { "Authorization", $"Bearer {accessToken}" } }
        };
        return await _client.SendAsync(request);
    }

    private async Task<string?> GetOpenIdAsync(string accessToken)
    {
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder("https://graph.qq.com/oauth2.0/me")
            {
                Query = new QueryBuilder { { "access_token", accessToken }, { "fmt", "json" } }.ToString()
            }.Uri,
            Headers = { { "Authorization", $"Bearer {accessToken}" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        var content = JsonConvert.DeserializeObject<QqOpenIdDto>(await response.Content.ReadAsStringAsync())!;
        Console.WriteLine(content.OpenId);
        return content.OpenId;
    }
}