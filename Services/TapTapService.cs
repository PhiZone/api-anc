using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class TapTapService : ITapTapService
{
    private readonly HttpClient _client;
    private readonly IOptions<TapTapSettings> _tapTapSettings;

    public TapTapService(IOptions<TapTapSettings> tapTapSettings)
    {
        _tapTapSettings = tapTapSettings;
        _client = new HttpClient { BaseAddress = new Uri(tapTapSettings.Value.TapApiUrl) };
    }

    public async Task<HttpResponseMessage?> Login(TapTapRequestDto dto)
    {
        if (dto.MacKey == null || dto.AccessToken == null)
        {
            return null;
        }
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
        request.Headers.TryAddWithoutValidation("Authorization",
            $"MAC id=\"{dto.AccessToken}\",ts=\"{timestamp}\",nonce=\"{nonce}\",mac=\"{hmac}\"");
        return await _client.SendAsync(request);
    }
}