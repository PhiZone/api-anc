using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using Role = PhiZoneApi.Constants.Role;

namespace PhiZoneApi.Services;

public partial class ResourceService : IResourceService
{
    private readonly UserManager<User> _userManager;
    private readonly ISongRepository _songRepository;
    private readonly ISongSubmissionRepository _songSubmissionRepository;
    private readonly IChartRepository _chartRepository;

    public ResourceService(UserManager<User> userManager, ISongRepository songRepository,
        ISongSubmissionRepository songSubmissionRepository, IChartRepository chartRepository)
    {
        _userManager = userManager;
        _songRepository = songRepository;
        _songSubmissionRepository = songSubmissionRepository;
        _chartRepository = chartRepository;
    }

    public string GetRichText<T>(string id, string display)
    {
        return $"[PZ{typeof(T).Name}:{id}:{display}:PZRT]";
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
        return $"{title} [{chart.Level} {Math.Floor(chart.Difficulty)}] {record.Score} {record.Accuracy:P}";
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

    public async Task<bool> HasPermission(User user, Role targetRole)
    {
        var currentRole = await GetRole(user);
        return currentRole!.Priority >= targetRole.Priority;
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

    [GeneratedRegex("\\[PZUser:[0-9]+:")]
    private static partial Regex UserRegex();
}