using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable TailRecursiveCall

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class TapGhostService : ITapGhostService
{
    private readonly HttpClient _client;
    private readonly ILogger<TapGhostService> _logger;
    private readonly string _queue;
    private readonly IRabbitMqService _rabbitMqService;
    private readonly IOptions<TapGhostSettings> _tapGhostSettings;
    private readonly ITokenService _tokenService;
    private string? _token;

    public TapGhostService(IOptions<TapGhostSettings> tapGhostSettings, IRabbitMqService rabbitMqService,
        ITokenService tokenService, IHostEnvironment env, ILogger<TapGhostService> logger)
    {
        _tapGhostSettings = tapGhostSettings;
        _tokenService = tokenService;
        _logger = logger;
        _rabbitMqService = rabbitMqService;
        _queue = env.IsProduction() ? "tap-record" : "tap-record-dev";
        _client = new HttpClient { BaseAddress = new Uri(tapGhostSettings.Value.ApiUrl) };
        Task.Run(() => UpdateToken(true));
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
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await GetGhost(appId, id);
            }

            if (response.StatusCode != HttpStatusCode.NotFound)
                _logger.LogError(LogEvents.TapGhostFailure,
                    "An error occurred whilst retrieving ghost ({Status}):\n{Error}", response.StatusCode,
                    await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<ResponseDto<TapGhost>>(await response.Content.ReadAsStringAsync())!.Data;
    }

    public async Task<IEnumerable<Record>?> GetRecords(Guid appId, string id)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new UriBuilder($"{_tapGhostSettings.Value.ApiUrl}/records/{appId}/{id}")
            {
                Query = new QueryBuilder { { "PerPage", "-1" } }.ToString()
            }.Uri,
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await GetRecords(appId, id);
            }

            _logger.LogError(LogEvents.TapGhostFailure,
                "An error occurred whilst retrieving records ({Status}):\n{Error}", response.StatusCode,
                await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<ResponseDto<IEnumerable<Record>>>(
            await response.Content.ReadAsStringAsync())!.Data;
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
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await ModifyGhost(ghost);
            }

            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst modifying ghost ({Status}):\n{Error}",
                response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        return response;
    }

    public async Task<double> CreateRecord(Guid appId, string id, Record record, bool isChartRanked)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new UriBuilder($"{_tapGhostSettings.Value.ApiUrl}/records/{appId}/{id}")
            {
                Query = new QueryBuilder { { "IsChartRanked", isChartRanked.ToString() } }.ToString()
            }.Uri,
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(record)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await CreateRecord(appId, id, record, isChartRanked);
            }

            _logger.LogError(LogEvents.TapGhostFailure, "An error occurred whilst creating record ({Status}):\n{Error}",
                response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        return JsonConvert.DeserializeObject<ResponseDto<RksDto>>(await response.Content.ReadAsStringAsync())!.Data!
            .Rks;
    }

    public void PublishRecord(Guid appId, string id, Record record, bool isChartRanked, ulong experienceDelta)
    {
        using var channel = _rabbitMqService.GetConnection().CreateModel();
        var properties = channel.CreateBasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            { "AppId", appId.ToString() }, { "Id", id }, { "IsChartRanked", isChartRanked.ToString() },
            { "ExpDelta", experienceDelta.ToString() }
        };
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(record));
        channel.BasicPublish("", _queue, false, properties, body);
    }

    private async Task UpdateToken(bool force = false)
    {
        var token = _tokenService.GetToken(CriticalValues.TapGhostServiceId, TimeSpan.FromHours(5.9));
        if (!force && token != null)
        {
            _token = token;
            return;
        }

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

        var data =
            JsonConvert.DeserializeObject<OpenIddictTokenResponseDto>(await response.Content.ReadAsStringAsync())!;
        _token = data.AccessToken;
        _tokenService.UpdateToken(CriticalValues.TapGhostServiceId, _token);
    }
}