using System.Net;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;

// ReSharper disable TailRecursiveCall

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class PhigrimService : IPhigrimService
{
    private readonly HttpClient _client;
    private readonly ILogger<PhigrimService> _logger;
    private readonly IOptions<PhigrimSettings> _phigrimSettings;
    private readonly ITokenService _tokenService;
    private string? _token;

    public PhigrimService(IOptions<PhigrimSettings> phigrimSettings, ITokenService tokenService,
        ILogger<PhigrimService> logger)
    {
        _phigrimSettings = phigrimSettings;
        _tokenService = tokenService;
        _logger = logger;
        _client = new HttpClient { BaseAddress = new Uri(phigrimSettings.Value.ApiUrl) };
        Task.Run(() => UpdateToken(true));
    }

    public async Task<PhigrimInheritanceDto?> GetInheritingUser(TapTapGhostInheritanceDelivererDto dto)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_phigrimSettings.Value.ApiUrl}/me/bindings/tapTap/inherit"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(dto)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await GetInheritingUser(dto);
            }

            if (response.StatusCode != HttpStatusCode.NotFound)
                _logger.LogError(LogEvents.PhigrimFailure,
                    "An error occurred whilst retrieving ghost ({Status}):\n{Error}", response.StatusCode,
                    await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<ResponseDto<PhigrimInheritanceDto>>(
            await response.Content.ReadAsStringAsync())!.Data;
    }

    public async Task<ResponseDto<IEnumerable<RecordDto>>?> GetRecords(int remoteId, int? page = null,
        int? perPage = null)
    {
        await UpdateToken();
        var query = new QueryBuilder { { "RangeOwnerId", remoteId.ToString() } };
        if (page != null) query.Add("Page", page.ToString()!);

        if (perPage != null) query.Add("PerPage", perPage.ToString()!);

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri =
                new UriBuilder($"{_phigrimSettings.Value.ApiUrl}/records") { Query = query.ToString() }.Uri,
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await UpdateToken(true);
                return await GetRecords(remoteId, page, perPage);
            }

            _logger.LogError(LogEvents.PhigrimFailure,
                "An error occurred whilst retrieving records ({Status}):\n{Error}", response.StatusCode,
                await response.Content.ReadAsStringAsync());
            return null;
        }

        return JsonConvert.DeserializeObject<ResponseDto<IEnumerable<RecordDto>>>(
            await response.Content.ReadAsStringAsync());
    }

    private async Task UpdateToken(bool force = false)
    {
        var token = _tokenService.GetToken(CriticalValues.PhigrimServiceId, TimeSpan.FromHours(5.9));
        if (!force && token != null)
        {
            _token = token;
            return;
        }

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_phigrimSettings.Value.ApiUrl}/auth/token"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _phigrimSettings.Value.ClientId },
                { "client_secret", _phigrimSettings.Value.ClientSecret },
                { "grant_type", OpenIddictConstants.GrantTypes.ClientCredentials }
            })
        };
        var response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation(LogEvents.PhigrimInfo, "Successfully updated access token");
        else
            _logger.LogError(LogEvents.PhigrimFailure, "An error occurred whilst updating access token:\n{Error}",
                await response.Content.ReadAsStringAsync());

        var data =
            JsonConvert.DeserializeObject<OpenIddictTokenResponseDto>(await response.Content.ReadAsStringAsync())!;
        _token = data.AccessToken;
        _tokenService.UpdateToken(CriticalValues.PhigrimServiceId, _token);
    }
}