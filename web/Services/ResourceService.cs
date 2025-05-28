using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Ganss.Xss;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using OpenIddict.Abstractions;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using StackExchange.Redis;

namespace PhiZoneApi.Services;

public partial class ResourceService(IServiceProvider serviceProvider, IConfiguration configuration) : IResourceService
{
    private readonly ResourceDto _resourceDto = JsonConvert.DeserializeObject<ResourceDto>(File.ReadAllText(
        Path.Combine(Directory.GetCurrentDirectory(),
            configuration.GetSection("ResourceSettings").GetValue<string>("DirectoryPath") ?? "Resources",
            "resources.json")))!;

    private readonly Dictionary<string, Guid> _userSubmissionSessionDictionary = new();

    public string GetRichText<T>(string id, string display, string? addition = null)
    {
        return $"[PZ{typeof(T).Name}{addition}:{id}:{display}:PZRT]";
    }

    public string GetComplexRichText(string type, string id1, string id2, string display, string? addition = null)
    {
        return $"[PZ{type}{addition}:{id1}:{id2}:{display}:PZCRT]";
    }

    public async Task<string> GetDisplayName(Chart chart)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        var title = chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title;
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    public async Task<string> GetDisplayName(ChartSubmission chart)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        var songSubmissionRepository = scope.ServiceProvider.GetRequiredService<ISongSubmissionRepository>();
        var title = chart.Title ?? (chart.SongId != null
            ? (await songRepository.GetSongAsync(chart.SongId.Value)).Title
            : (await songSubmissionRepository.GetSongSubmissionAsync(chart.SongSubmissionId!.Value)).Title);
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    public async Task<string> GetDisplayName(Record record)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        var songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        var chart = await chartRepository.GetChartAsync(record.ChartId);
        var title = chart.Title ?? (await songRepository.GetSongAsync(chart.SongId)).Title;
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}] {record.Score} {record.Accuracy:P2}";
    }

    public List<int> GetAuthorIds(string name)
    {
        var result = new List<int>();
        foreach (Match match in UserRichTextRegex().Matches(name))
        {
            var parts = match.Value.Split(':');
            result.Add(int.Parse(parts[1]));
        }

        return result;
    }

    public string FromRichText(string input)
    {
        return string.IsNullOrEmpty(input)
            ? input
            : GeneralRichTextRegex().Replace(input, match => match.Groups[3].Value);
    }

    public async Task<bool> IsBlacklisted(int user1, int user2)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var userRelationRepository = scope.ServiceProvider.GetRequiredService<IUserRelationRepository>();
        if (await userRelationRepository.RelationExistsAsync(user1, user2))
            if ((await userRelationRepository.GetRelationAsync(user1, user2)).Type == UserRelationType.Blacklisted)
                return true;
        if (!await userRelationRepository.RelationExistsAsync(user2, user1)) return false;
        return (await userRelationRepository.GetRelationAsync(user2, user1)).Type == UserRelationType.Blacklisted;
    }

    public async Task<(bool, bool)> IsPreparedOrFinished(EventTeam eventTeam)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var participationRepository = scope.ServiceProvider.GetRequiredService<IParticipationRepository>();
        var eventResourceRepository = scope.ServiceProvider.GetRequiredService<IEventResourceRepository>();
        return (
            eventTeam.ClaimedParticipantCount ==
            await participationRepository.CountParticipationsAsync(e => e.TeamId == eventTeam.Id),
            eventTeam.ClaimedSubmissionCount == await eventResourceRepository.CountResourcesAsync(eventTeam.DivisionId,
                e => e.Type == EventResourceType.Entry && e.TeamId == eventTeam.Id));
    }

    public bool HasPermission(User? user, UserRole role)
    {
        return user != null && GetPriority(user.Role) >= GetPriority(role);
    }

    public void JoinSession(string connectionId, Guid sessionId)
    {
        _userSubmissionSessionDictionary[connectionId] = sessionId;
    }

    public Guid? GetSessionId(string connectionId)
    {
        return _userSubmissionSessionDictionary.TryGetValue(connectionId, out var sessionId) ? sessionId : null;
    }

    public void LeaveSession(string connectionId)
    {
        _userSubmissionSessionDictionary.Remove(connectionId);
    }

    public async Task CleanupSession(Guid id)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        var key = $"phizone:session:submission:{id}";
        if (!await db.KeyExistsAsync(key)) return;

        var session = JsonConvert.DeserializeObject<SubmissionSession>((await db.StringGetAsync(key))!)!;
        CleanupSession(session);
        await db.KeyDeleteAsync(key);
    }

    public void CleanupSession(SubmissionSession session)
    {
        if (Path.Exists(session.SongPath)) File.Delete(session.SongPath);
        if (Path.Exists(session.IllustrationPath)) File.Delete(session.IllustrationPath);
        if (Path.Exists(session.SongPath + ".wav")) File.Delete(session.SongPath + ".wav");
    }

    public async Task<SongRecognitionSummaryDto> GenerateMatchSummary(IEnumerable<SeekTuneFindResult> songResults,
        IEnumerable<SeekTuneFindResult> resourceRecordResults, User currentUser)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        var songSubmissionRepository = scope.ServiceProvider.GetRequiredService<ISongSubmissionRepository>();
        var resourceRecordRepository = scope.ServiceProvider.GetRequiredService<IResourceRecordRepository>();
        var dtoMapper = scope.ServiceProvider.GetRequiredService<IDtoMapper>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var songIds = new List<Guid>();
        var songResultsList = songResults.ToList();
        var songSubmissionIds = songResultsList.Select(e => e.Id);
        var songSubmissions =
            await songSubmissionRepository.GetSongSubmissionsAsync(predicate: e => songSubmissionIds.Contains(e.Id));
        foreach (var songSubmission in songSubmissions)
            if (songSubmission.RepresentationId != null)
                songIds.Add(songSubmission.RepresentationId!.Value);

        var songMatches =
            (await songRepository.GetSongsAsync(predicate: e =>
                songIds.Contains(e.Id) && (e.Accessibility != Accessibility.RefuseAny || e.OwnerId == currentUser.Id)))
            .Select(e => dtoMapper.MapSong<SongMatchDto>(e))
            .Select(e =>
            {
                e.Score = songResultsList.First(f =>
                        f.Id == e.Id || f.Id == songSubmissions
                            .First(g => g.RepresentationId != null && g.RepresentationId.Value == e.Id)
                            .Id)
                    .Score;
                return e;
            });
        var songSubmissionMatches = songSubmissions
            .Where(e => e.RepresentationId == null &&
                        (e.Accessibility != Accessibility.RefuseAny || e.OwnerId == currentUser.Id))
            .Select(e => dtoMapper.MapSongSubmission<SongSubmissionMatchDto>(e, currentUser))
            .Select(e =>
            {
                e.Score = songResultsList.First(f => f.Id == e.Id).Score;
                return e;
            });
        var resourceRecordResultsList = resourceRecordResults.ToList();
        var resourceRecordIds = resourceRecordResultsList.Select(e => e.Id);
        var resourceRecordMatches = mapper
            .Map<IEnumerable<ResourceRecordMatchDto>>(await resourceRecordRepository.GetResourceRecordsAsync(
                predicate: e => resourceRecordIds.Contains(e.Id) && e.Strategy != ResourceRecordStrategy.Accept))
            .Select(e =>
            {
                e.Score = resourceRecordResultsList.First(f => f.Id == e.Id).Score;
                return e;
            });

        return new SongRecognitionSummaryDto
        {
            SongMatches = songMatches.OrderByDescending(e => e.Score),
            SongSubmissionMatches = songSubmissionMatches.OrderByDescending(e => e.Score),
            ResourceRecordMatches = resourceRecordMatches.OrderByDescending(e => e.Score)
        };
    }

    public async Task<(string, List<User>)> ParseUserContent(string content)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var matches = UserMentionRichTextRegex().Matches(content);
        var result = content;
        var users = new List<User>();
        foreach (var userName in matches.Select(match => match.Groups[1].Value))
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user == null) continue;
            users.Add(user);
            result = result.Replace($"@{userName}", GetRichText<User>(user.Id.ToString(), user.UserName!, "Mention"));
        }

        return (result, users);
    }

    public string Normalize(string? input)
    {
        return input != null ? WhitespaceRegex().Replace(input, "").ToUpper() : "";
    }

    public bool IsUserNameValid(string userName)
    {
        return IsValid(userName, UserNameRegex());
    }

    public bool IsEmailValid(string email)
    {
        return IsValid(email, EmailRegex());
    }

    public async Task<bool> IsUserInputValidAsync(string input, string memberName)
    {
        if (input.Trim() == string.Empty) return true;

        if (!memberName.Equals("Code") && input.Contains('<') && input.Contains('>'))
        {
            var sanitizer = new HtmlSanitizer();
            if (sanitizer.Sanitize(input) != input) return false;
        }

        if (_resourceDto.ProhibitedWords.Any(word => input.Contains(word, StringComparison.CurrentCultureIgnoreCase)))
            return false;

        await using var scope = serviceProvider.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var request = httpContextAccessor.HttpContext!.Request;
        var userId = httpContextAccessor.HttpContext!.User.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = userId != null ? await userManager.FindByIdAsync(userId) : null;
        var messengerService = scope.ServiceProvider.GetRequiredService<IMessengerService>();
        await messengerService.SendUserInput(new UserInputDelivererDto
        {
            Content = input,
            IsImage = false,
            MemberName = memberName,
            ResourceId = (string?)request.RouteValues["id"],
            ActionName = (string)request.RouteValues["action"]!,
            ControllerName = (string)request.RouteValues["controller"]!,
            RequestMethod = request.Method,
            RequestPath = request.Path,
            UserId = userId != null ? int.Parse(userId) : null,
            UserName = user?.UserName,
            UserAvatar = user?.Avatar,
            DateCreated = DateTimeOffset.UtcNow
        });
        return true;
    }

    public async Task<string> GenerateLoginTokenAsync(User user, TimeSpan? expiry = null)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var redis = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
        var db = redis.GetDatabase();
        string token;
        string key;
        do
        {
            token = Guid.NewGuid().ToString();
            key = $"phizone:login:{token}";
        } while (await db.KeyExistsAsync(key));

        await db.StringSetAsync(key, user.Id.ToString(), expiry ?? TimeSpan.FromSeconds(30));
        return token;
    }

    public string GenerateCode(int length)
    {
        // const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        const string chars = "BDFHJLNPRTVXZBDFHJLNPRTVXZ0123456789";
        var code = new StringBuilder(length);
        var random = new Random();

        for (var i = 0; i < length; i++)
        {
            var index = random.Next(0, chars.Length);
            code.Append(chars[index]);
        }

        return code.ToString();
    }

    private static bool IsValid(string input, Regex regex)
    {
        if (string.IsNullOrEmpty(input)) return false;
        var enumerator = regex.EnumerateMatches((ReadOnlySpan<char>)input).GetEnumerator();
        if (!enumerator.MoveNext()) return false;
        var current = enumerator.Current;
        return current.Index == 0 && current.Length == input.Length;
    }

    private static int GetPriority(UserRole role)
    {
        return role switch
        {
            UserRole.Administrator => 6,
            UserRole.Moderator => 5,
            UserRole.Volunteer => 4,
            UserRole.Qualified => 3,
            UserRole.Sponsor => 2,
            UserRole.Member => 1,
            _ => 0
        };
    }

    [GeneratedRegex(@"\s")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"\[PZUser:[0-9]+:")]
    private static partial Regex UserRichTextRegex();

    [GeneratedRegex(@"@([A-Za-z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]+)")]
    private static partial Regex UserMentionRichTextRegex();

    [GeneratedRegex(
        @"^([A-Za-z0-9_]{4,24}|[a-zA-Z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{3,12}|[\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]{2,12})$")]
    private static partial Regex UserNameRegex();

    [GeneratedRegex(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\[PZ([A-Za-z]+):([0-9]+):((?:(?!:PZRT\]).)*):PZRT\]", RegexOptions.IgnoreCase, "zh-CN")]
    private static partial Regex GeneralRichTextRegex();
}