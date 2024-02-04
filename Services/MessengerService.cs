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
    private DateTimeOffset _lastTokenUpdate;
    private string? _token;

    public MessengerService(IOptions<MessengerSettings> messengerSettings, ILogger<MessengerService> logger)
    {
        _messengerSettings = messengerSettings;
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
            _logger.LogError(LogEvents.MessengerFailure, "An error occurred whilst sending email:\n{Error}",
                await response.Content.ReadAsStringAsync());

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
            _logger.LogError(LogEvents.MessengerFailure, "An error occurred whilst sending user input:\n{Error}",
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
            RequestUri = new Uri($"{_messengerSettings.Value.ApiUrl}/auth/token"),
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "client_id", _messengerSettings.Value.ClientId },
                { "client_secret", _messengerSettings.Value.ClientSecret },
                { "grant_type", OpenIddictConstants.GrantTypes.ClientCredentials }
            })
        };
        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            _logger.LogError(LogEvents.MessengerFailure, "An error occurred whilst updating access token:\n{Error}",
                await response.Content.ReadAsStringAsync());

        var data =
            JsonConvert.DeserializeObject<OpenIddictTokenResponseDto>(await response.Content.ReadAsStringAsync())!;
        _token = data.AccessToken;
        _lastTokenUpdate = now;
    }
}