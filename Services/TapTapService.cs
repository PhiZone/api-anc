using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public partial class TapTapService : ITapTapService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly HttpClient _client;
    private readonly SignInManager<User> _signInManager;
    private readonly IOptions<TapTapSettings> _tapTapSettings;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly UserManager<User> _userManager;

    public TapTapService(IOptions<TapTapSettings> tapTapSettings, IOpenIddictTokenManager tokenManager,
        SignInManager<User> signInManager, IOpenIddictApplicationManager applicationManager,
        UserManager<User> userManager, IOpenIddictAuthorizationManager authorizationManager)
    {
        _tapTapSettings = tapTapSettings;
        _tokenManager = tokenManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _userManager = userManager;
        _authorizationManager = authorizationManager;
        _client = new HttpClient { BaseAddress = new Uri(tapTapSettings.Value.TapApiUrl) };
    }

    public async Task<HttpResponseMessage> Login(TapLoginRequestDto dto)
    {
        var nonceBytes = new byte[16];
        using var generator = RandomNumberGenerator.Create();
        generator.GetBytes(nonceBytes);
        var nonce = Convert.ToBase64String(nonceBytes);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var parts = _tapTapSettings.Value.TapApiUrl.Split("://");
        var path = $"/account/profile/v1?client_id={_tapTapSettings.Value.ClientId}";
        var signArray = new List<string>
        {
            timestamp,
            nonce,
            "GET",
            path,
            parts[1],
            parts[0].Length > 4 ? "443" : "80",
            ""
        };
        using var hasher = new HMACSHA1(Encoding.UTF8.GetBytes(dto.MacKey));
        var hmac = Convert.ToBase64String(
            await hasher.ComputeHashAsync(
                new MemoryStream(Encoding.UTF8.GetBytes($"{string.Join('\n', signArray)}\n"))));
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get, RequestUri = new Uri($"{_tapTapSettings.Value.TapApiUrl}{path}")
        };
        request.Headers.Authorization =
            new AuthenticationHeaderValue($"MAC id={dto.AccessToken},ts={timestamp},nonce={nonce},mac={hmac}");
        return await _client.SendAsync(request);
    }

    public string StandardizeUserName(string userName)
    {
        return InvalidUserNameRegex().Replace(userName, "");
    }

    // public async Task<(string, string)> GetTokens(User user)
    // {
    //     // Create a new ClaimsPrincipal containing the claims that
    //     // will be used to create an id_token and/or an access token.
    //     var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(
    //         new Claim[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) },
    //         OpenIddictServerAspNetCoreDefaults.AuthenticationScheme));
    //
    //     // Create a new authorization descriptor.
    //     var authorizationDescriptor = new OpenIddictAuthorizationDescriptor
    //     {
    //         ApplicationId = "4",
    //         Principal = claimsPrincipal,
    //         Status = OpenIddictConstants.Statuses.Valid,
    //         Type = OpenIddictConstants.AuthorizationTypes.Permanent,
    //         Scopes = { OpenIddictConstants.Scopes.Roles }
    //     };
    //
    //     // Create a new authorization.
    //     var authorization = (await _authorizationManager.CreateAsync(authorizationDescriptor) as OpenIddictEntityFrameworkCoreAuthorization)!;
    //     Console.WriteLine(authorization.GetType());
    //     
    //     // Create a new access token descriptor.
    //     var accessTokenDescriptor = new OpenIddictTokenDescriptor
    //     {
    //         AuthorizationId = authorization.Id,
    //         Principal = claimsPrincipal,
    //         Type = OpenIddictConstants.TokenTypeHints.AccessToken,
    //         CreationDate = DateTime.Now,
    //         ExpirationDate = DateTime.Now + TimeSpan.FromHours(2)
    //     };
    //
    //     // Create a new access token.
    //     var accessToken = await _tokenManager.CreateAsync(accessTokenDescriptor) as OpenIddictEntityFrameworkCoreToken;
    //
    //     // Create a new refresh token descriptor.
    //     var refreshTokenDescriptor = new OpenIddictTokenDescriptor
    //     {
    //         AuthorizationId = authorization.Id,
    //         Principal = claimsPrincipal,
    //         Type = OpenIddictConstants.TokenTypeHints.RefreshToken,
    //         CreationDate = DateTime.Now,
    //         ExpirationDate = DateTime.Now + TimeSpan.FromDays(14)
    //     };
    //
    //     // Create a new refresh token.
    //     var refreshToken = await _tokenManager.CreateAsync(refreshTokenDescriptor) as OpenIddictEntityFrameworkCoreToken;
    //
    //     return (accessToken!.ReferenceId!, refreshToken!.ReferenceId!);
    // }

    // public async Task<(string, string)> GetTokens(User user)
    // {
    //     var application = ((await _applicationManager.FindByIdAsync("4"))! as OpenIddictEntityFrameworkCoreApplication<int>)!;
    //     var accessToken = new OpenIddictEntityFrameworkCoreToken<int>
    //     {
    //         Application = application,
    //         Subject = user.Id.ToString(),
    //         Type = OpenIddictConstants.TokenTypeHints.AccessToken,
    //         ReferenceId = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)),
    //         CreationDate = DateTime.UtcNow,
    //         ExpirationDate = DateTime.UtcNow.AddHours(2)
    //     };
    //     accessToken.Payload = GeneratePayload(user.Id.ToString(), accessToken.ExpirationDate.Value, application.ClientSecret!);
    //     var refreshToken = new OpenIddictEntityFrameworkCoreToken<int>
    //     {
    //         Application = application,
    //         Subject = user.Id.ToString(),
    //         Type = OpenIddictConstants.TokenTypeHints.RefreshToken,
    //         ReferenceId = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)),
    //         CreationDate = DateTime.UtcNow,
    //         ExpirationDate = DateTime.UtcNow.AddDays(14)
    //     };
    //     refreshToken.Payload = GeneratePayload(user.Id.ToString(), refreshToken.ExpirationDate.Value, application.ClientSecret!);
    //     await _tokenManager.CreateAsync(accessToken);
    //     await _tokenManager.CreateAsync(refreshToken);
    //     return (accessToken.ReferenceId!, refreshToken.ReferenceId!);
    // }

    [GeneratedRegex(@"[^a-zA-Z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]")]
    private static partial Regex InvalidUserNameRegex();
}