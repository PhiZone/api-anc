using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PhiZoneApi.Configurations;
using PhiZoneApi.Constants;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Requests;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class FeishuService : IFeishuService
{
    private readonly HttpClient _client;
    private readonly IConfiguration _config;
    private readonly IOptions<FeishuSettings> _feishuSettings;
    private readonly ILogger<FeishuService> _logger;
    private readonly IServiceProvider _provider;
    private DateTimeOffset _lastTokenUpdate;
    private string? _token;

    public FeishuService(IOptions<FeishuSettings> feishuSettings, IConfiguration config, IServiceProvider provider,
        ILogger<FeishuService> logger)
    {
        _feishuSettings = feishuSettings;
        _config = config;
        _provider = provider;
        _logger = logger;
        _client = new HttpClient { BaseAddress = new Uri(feishuSettings.Value.ApiUrl) };
        Task.Run(UpdateToken);
    }

    public async Task Notify(SongSubmission submission, params int[] chats)
    {
        await UpdateToken();
        await using var scope = _provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<User>>()!;
        var variables = new Dictionary<string, string>
        {
            { "song", submission.Title },
            { "uploader", (await userManager.FindByIdAsync(submission.OwnerId.ToString()))!.UserName! },
            { "originality", submission.OriginalityProof != null ? "\u2705" : "\u274c" },
            { "composer", submission.AuthorName },
            { "illustrator", submission.Illustrator },
            {
                "bpm",
                Math.Abs(submission.MinBpm - submission.MaxBpm) < 1e-5
                    ? submission.Bpm.ToString(CultureInfo.InvariantCulture)
                    : $"{submission.Bpm} ({submission.MinBpm} ~ {submission.MaxBpm})"
            },
            { "duration", submission.Duration!.Value.ToString(@"mm\:ss") },
            { "submission_info", $"{_config["WebsiteUrl"]}/studio/song-submissions/{submission.Id}" }
        };
        var content =
            $"{{\"type\":\"template\",\"data\":{{\"template_id\":\"{_feishuSettings.Value.Cards[FeishuResources.SongCard]}\",\"template_variable\":{JsonConvert.SerializeObject(variables)}}}}}";
        foreach (var chat in chats)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri =
                    new Uri($"{_feishuSettings.Value.ApiUrl}/open-apis/im/v1/messages?receive_id_type=chat_id"),
                Headers = { { "Authorization", $"Bearer {_token}" } },
                Content = JsonContent.Create(new FeishuMessageDto
                {
                    ReceiveId = _feishuSettings.Value.Chats[chat],
                    MessageType = "interactive",
                    Content = content
                })
            };
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogError(LogEvents.FeishuFailure,
                    "[{Now}] An error occurred whilst announcing song update:\n{Error}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), await response.Content.ReadAsStringAsync());
        }
    }

    public async Task Notify(ChartSubmission submission, params int[] chats)
    {
        await UpdateToken();
        await using var scope = _provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<User>>()!;
        var songRepository = scope.ServiceProvider.GetService<ISongRepository>()!;
        var songSubmissionRepository = scope.ServiceProvider.GetService<ISongSubmissionRepository>()!;
        string title;
        if (submission.SongId != null)
        {
            var song = await songRepository.GetSongAsync(submission.SongId.Value);
            title = song.Title;
        }
        else
        {
            var song = await songSubmissionRepository.GetSongSubmissionAsync(submission.SongSubmissionId!.Value);
            title = song.Title;
        }

        var variables = new Dictionary<string, string>
        {
            { "chart", $"{title} [{submission.Level} {Math.Floor(submission.Difficulty)}]" },
            { "uploader", (await userManager.FindByIdAsync(submission.OwnerId.ToString()))!.UserName! },
            { "is_ranked", submission.IsRanked ? "\u2705" : "\u274c" },
            { "level", $"[{submission.LevelType.ToString()}] {submission.Level}" },
            { "difficulty", submission.Difficulty.ToString(CultureInfo.InvariantCulture) },
            { "format", submission.Format.ToString() },
            { "note_count", submission.NoteCount.ToString() },
            { "submission_info", $"{_config["WebsiteUrl"]}/studio/chart-submissions/{submission.Id}" }
        };
        var content =
            $"{{\"type\":\"template\",\"data\":{{\"template_id\":\"{_feishuSettings.Value.Cards[FeishuResources.ChartCard]}\",\"template_variable\":{JsonConvert.SerializeObject(variables)}}}}}";
        foreach (var chat in chats)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri =
                    new Uri($"{_feishuSettings.Value.ApiUrl}/open-apis/im/v1/messages?receive_id_type=chat_id"),
                Headers = { { "Authorization", $"Bearer {_token}" } },
                Content = JsonContent.Create(new FeishuMessageDto
                {
                    ReceiveId = _feishuSettings.Value.Chats[chat],
                    MessageType = "interactive",
                    Content = content
                })
            };
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogError(LogEvents.FeishuFailure,
                    "[{Now}] An error occurred whilst announcing chart update:\n{Error}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), await response.Content.ReadAsStringAsync());
        }
    }

    public async Task Notify(PetAnswer answer, DateTimeOffset dateStarted, params int[] chats)
    {
        await UpdateToken();
        await using var scope = _provider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetService<UserManager<User>>()!;

        var duration = DateTimeOffset.UtcNow - dateStarted;

        var variables = new Dictionary<string, string>
        {
            { "answerer", (await userManager.FindByIdAsync(answer.OwnerId.ToString()))!.UserName! },
            { "objective_score", answer.ObjectiveScore.ToString() },
            { "time", dateStarted.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss") },
            { "duration", duration.ToString(@"mm\:ss") },
            { "answer_info", $"{_config["WebsiteUrl"]}/pet/answers/{answer.Id}" }
        };
        var content =
            $"{{\"type\":\"template\",\"data\":{{\"template_id\":\"{_feishuSettings.Value.Cards[FeishuResources.PetAnswerCard]}\",\"template_variable\":{JsonConvert.SerializeObject(variables)}}}}}";
        foreach (var chat in chats)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri =
                    new Uri($"{_feishuSettings.Value.ApiUrl}/open-apis/im/v1/messages?receive_id_type=chat_id"),
                Headers = { { "Authorization", $"Bearer {_token}" } },
                Content = JsonContent.Create(new FeishuMessageDto
                {
                    ReceiveId = _feishuSettings.Value.Chats[chat],
                    MessageType = "interactive",
                    Content = content
                })
            };
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogError(LogEvents.FeishuFailure,
                    "[{Now}] An error occurred whilst announcing PET answer update:\n{Error}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), await response.Content.ReadAsStringAsync());
        }
    }

    private async Task UpdateToken()
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastTokenUpdate <= TimeSpan.FromHours(1.8))
        {
            _logger.LogInformation(LogEvents.FeishuInfo,
                "[{Now}] Skipping token update since it was last updated at {LastUpdate}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _lastTokenUpdate.ToString("yyyy-MM-dd HH:mm:ss"));
            return;
        }

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{_feishuSettings.Value.ApiUrl}/open-apis/auth/v3/tenant_access_token/internal"),
            Content = JsonContent.Create(new FeishuTokenDto
            {
                AppId = _feishuSettings.Value.AppId, AppSecret = _feishuSettings.Value.AppSecret
            })
        };
        var response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(LogEvents.FeishuInfo,
                "[{Now}] Successfully updated tenant token since its last update at {LastUpdate}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _lastTokenUpdate.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        else
        {
            _logger.LogError(LogEvents.FeishuFailure,
                "[{Now}] An error occurred whilst updating tenant token:\n{Error}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), await response.Content.ReadAsStringAsync());
        }

        var data = JsonConvert.DeserializeObject<FeishuTokenDelivererDto>(await response.Content.ReadAsStringAsync())!;
        _token = data.TenantAccessToken;
        _lastTokenUpdate = now;
    }
}