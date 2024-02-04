using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public partial class ResourceService(
    IServiceProvider serviceProvider,
    IConfiguration configuration)
    : IResourceService
{
    private readonly ResourceDto _resourceDto = JsonConvert.DeserializeObject<ResourceDto>(File.ReadAllText(
        Path.Combine(Directory.GetCurrentDirectory(),
            configuration.GetSection("ResourceSettings").GetValue<string>("DirectoryPath")!, "resources.json")))!;

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

    public bool HasPermission(User user, UserRole role)
    {
        return GetPriority(user.Role) >= GetPriority(role);
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

    public string Normalize(string input)
    {
        return WhitespaceRegex().Replace(input, "").ToUpper();
    }

    public ResourceDto GetResources()
    {
        return _resourceDto;
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
}