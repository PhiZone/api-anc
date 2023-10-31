using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using Role = PhiZoneApi.Constants.Role;

namespace PhiZoneApi.Services;

public partial class ResourceService : IResourceService
{
    private readonly IChartRepository _chartRepository;
    private readonly ISongRepository _songRepository;
    private readonly ISongSubmissionRepository _songSubmissionRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public ResourceService(UserManager<User> userManager, ISongRepository songRepository,
        ISongSubmissionRepository songSubmissionRepository, IChartRepository chartRepository,
        IUserRelationRepository userRelationRepository)
    {
        _userManager = userManager;
        _songRepository = songRepository;
        _songSubmissionRepository = songSubmissionRepository;
        _chartRepository = chartRepository;
        _userRelationRepository = userRelationRepository;
    }

    public string GetRichText<T>(string id, string display, string? addition = null)
    {
        return $"[PZ{typeof(T).Name}{addition}:{id}:{display}:PZRT]";
    }

    public string GetComplexRichText<T>(string id1, string id2, string display)
    {
        return $"[PZ{typeof(T).Name}:{id1}:{id2}:{display}:PZCRT]";
    }

    public async Task<string> GetDisplayName(Chart chart)
    {
        var title = chart.Title ?? (await _songRepository.GetSongAsync(chart.SongId)).Title;
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    public async Task<string> GetDisplayName(ChartSubmission chart)
    {
        var title = chart.Title ?? (chart.SongId != null
            ? (await _songRepository.GetSongAsync(chart.SongId.Value)).Title
            : (await _songSubmissionRepository.GetSongSubmissionAsync(chart.SongSubmissionId!.Value)).Title);
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}]";
    }

    public async Task<string> GetDisplayName(Record record)
    {
        var chart = await _chartRepository.GetChartAsync(record.ChartId);
        var title = chart.Title ?? (await _songRepository.GetSongAsync(chart.SongId)).Title;
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}] {record.Score} {record.Accuracy:P2}";
    }

    public List<int> GetAuthorIds(string name)
    {
        var result = new List<int>();
        foreach (Match match in UserRegex().Matches(name))
        {
            var parts = match.Value.Split(':');
            result.Add(int.Parse(parts[1]));
        }

        return result;
    }

    public async Task<bool> IsBlacklisted(int user1, int user2)
    {
        if (await _userRelationRepository.RelationExistsAsync(user1, user2))
            if ((await _userRelationRepository.GetRelationAsync(user1, user2)).Type == UserRelationType.Blacklisted)
                return true;
        if (!await _userRelationRepository.RelationExistsAsync(user2, user1)) return false;
        return (await _userRelationRepository.GetRelationAsync(user2, user1)).Type == UserRelationType.Blacklisted;
    }

    public async Task<bool> HasPermission(User user, Role targetRole)
    {
        var currentRole = await GetRole(user);
        return currentRole != null && currentRole.Priority >= targetRole.Priority;
    }

    public async Task<bool> HasPermission(User user, int priority)
    {
        var currentRole = await GetRole(user);
        return currentRole!.Priority >= priority;
    }

    public async Task<Role?> GetRole(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count < 1) return null;
        var roleName = roles.First();
        return Roles.List.FirstOrDefault(role => role.Name == roleName);
    }

    public async Task<(string, List<User>)> ParseUserContent(string content)
    {
        var matches = UserMentionRegex().Matches(content);
        var result = content;
        var users = new List<User>();
        foreach (var userName in matches.Select(match => match.Groups[1].Value))
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) continue;
            users.Add(user);
            result = result.Replace($"@{userName}", GetRichText<User>(user.Id.ToString(), user.UserName!, "Mention"));
        }

        return (result, users);
    }

    [GeneratedRegex("\\[PZUser:[0-9]+:")]
    private static partial Regex UserRegex();

    [GeneratedRegex(@"@([A-Za-z0-9_\u4e00-\u9fff\u3041-\u309f\u30a0-\u30ff\uac00-\ud7a3]+)")]
    private static partial Regex UserMentionRegex();
}