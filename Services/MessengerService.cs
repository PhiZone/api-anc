using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class MessengerService : IMessengerService
{
    private readonly HttpClient _client;
    private readonly ILogger<MessengerService> _logger;
    private readonly IOptions<MessengerSettings> _messengerSettings;
    private readonly ITokenService _tokenService;
    private string? _token;

    public MessengerService(IOptions<MessengerSettings> messengerSettings, ITokenService tokenService,
        ILogger<MessengerService> logger)
    {
        _messengerSettings = messengerSettings;
        _tokenService = tokenService;
        _logger = logger;
        _client = new HttpClient { BaseAddress = new Uri(messengerSettings.Value.ApiUrl) };
        Task.Run(UpdateToken);
    }

    public async Task<HttpResponseMessage> SendMail(MailTaskDto dto)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_messengerSettings.Value.ApiUrl}/sendEmail"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(dto)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.MessengerFailure, "An error occurred whilst sending email ({Status}):\n{Error}",
                response.StatusCode, await response.Content.ReadAsStringAsync());

        return response;
    }

    public async Task<HttpResponseMessage> SendUserInput(UserInputDelivererDto dto)
    {
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_messengerSettings.Value.ApiUrl}/userInputs"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(dto)
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.MessengerFailure,
                "An error occurred whilst sending user input ({Status}):\n{Error}", response.StatusCode,
                await response.Content.ReadAsStringAsync());

        return response;
    }

    public async Task<HttpResponseMessage> Proxy(HttpRequestMessage message)
    {
        var dto = new ProxyRequestDto
        {
            Uri = message.RequestUri!.AbsoluteUri,
            Method = message.Method.Method,
            Headers = message.Headers.Select(header => new HeaderDto { Key = header.Key, Values = header.Value }),
            ContentType = message.Content?.Headers.ContentType?.MediaType,
            Body = message.Content != null ? await message.Content.ReadAsStringAsync() : null
        };
        await UpdateToken();
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_messengerSettings.Value.ApiUrl}/proxy"),
            Headers = { { "Authorization", $"Bearer {_token}" } },
            Content = JsonContent.Create(dto)
        };
        _logger.LogDebug(LogEvents.MessengerDebug, "Proxying {Method} {Uri} through {ActualUri} with content {Content}",
            dto.Method, dto.Uri, request.RequestUri, await request.Content.ReadAsStringAsync());
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.MessengerFailure,
                "An error occurred whilst sending proxy request ({Status}):\n{Error}", response.StatusCode,
                await response.Content.ReadAsStringAsync());

        return response;
    }

    private async Task UpdateToken()
    {
        var token = _tokenService.GetToken(CriticalValues.MessengerServiceId, TimeSpan.FromHours(5.9));
        if (token != null)
        {
            _token = token;
            return;
        }

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_messengerSettings.Value.ApiUrl}/auth/token"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _messengerSettings.Value.ClientId },
                { "client_secret", _messengerSettings.Value.ClientSecret },
                { "grant_type", OpenIddictConstants.GrantTypes.ClientCredentials }
            })
        };
        var response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
            _logger.LogInformation(LogEvents.MessengerInfo, "Successfully updated access token");
        else
            _logger.LogError(LogEvents.MessengerFailure,
                "An error occurred whilst updating access token ({Status}):\n{Error}", response.StatusCode,
                await response.Content.ReadAsStringAsync());

        var data =
            JsonConvert.DeserializeObject<OpenIddictTokenResponseDto>(await response.Content.ReadAsStringAsync())!;
        _token = data.AccessToken;
        _tokenService.UpdateToken(CriticalValues.MessengerServiceId, _token);
    }
}