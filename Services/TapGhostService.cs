using System.Net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class TapGhostService : ITapGhostService
{
    private readonly HttpClient _client;
    private readonly ILogger<TapGhostService> _logger;
    private readonly IOptions<TapGhostSettings> _tapGhostSettings;
    private DateTimeOffset _lastTokenUpdate;
    private string? _token;

    public TapGhostService(IOptions<TapGhostSettings> tapGhostSettings, ILogger<TapGhostService> logger)
    {
        _tapGhostSettings = tapGhostSettings;
        _logger = logger;
        _client = new HttpClient { BaseAddress = new Uri(tapGhostSettings.Value.ApiUrl) };
        Task.Run(UpdateToken);
    }

    public async Task<TapGhost?> GetGhost(Guid appId, string id)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_tapGhostSettings.Value.ApiUrl}/ghosts/{appId}/{id}"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode != HttpStatusCode.NotFound)
                _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst retrieving ghost:\n{Error}",
                    await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<TapGhost>(await response.Content.ReadAsStringAsync())!;
    }

    public async Task<IEnumerable<Record>?> GetRecords(Guid appId, string id)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{_tapGhostSettings.Value.ApiUrl}/records/{appId}/{id}"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst retrieving records:\n{Error}",
                await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<IEnumerable<Record>>(await response.Content.ReadAsStringAsync())!;
    }

    public async Task<HttpResponseMessage> ModifyGhost(TapGhost ghost)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_tapGhostSettings.Value.ApiUrl}/ghosts"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(ghost)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst modifying ghost:\n{Error}",
                await response.Content.ReadAsStringAsync());

        return response;
    }

    public async Task<HttpResponseMessage> CreateRecord(Guid appId, string id, Record record)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_tapGhostSettings.Value.ApiUrl}/records/{appId}/{id}"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(record)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst creating record:\n{Error}",
                await response.Content.ReadAsStringAsync());

        return response;
    }

    private async Task UpdateToken()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastTokenUpdate <= TimeSpan.FromHours(5.9)) return;

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_tapGhostSettings.Value.ApiUrl}/auth/token"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _tapGhostSettings.Value.ClientId },
                { "client_secret", _tapGhostSettings.Value.ClientSecret },
                { "grant_type", OpenIddictConstants.GrantTypes.ClientCredentials }
            })
        };
        var response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation(LogEvents.TapGhostInfo, "Successfully updated access token");
        else
            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst updating access token:\n{Error}",
                await response.Content.ReadAsStringAsync());

        var content = await response.Content.ReadAsStringAsync();
        var data =
            JsonConvert.DeserializeObject<OpenIddictTokenResponseDto>(content)!;
        _logger.LogInformation(content);
        _logger.LogInformation(data.AccessToken);
        _token = data.AccessToken;
        _lastTokenUpdate = now;
    }
}