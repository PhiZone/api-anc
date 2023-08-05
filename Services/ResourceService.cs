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

    public ResourceService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public string GetRichText<T>(string id, string display)
    {
        return $"[PZ{typeof(T).Name}:{id}:{display}:PZRT]";
    }

    public string GetComplexRichText<T>(string id1, string id2, string display)
    {
        return $"[PZ{typeof(T).Name}:{id1}:{id2}:{display}:PZCRT]";
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
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count < 1) return false;
        var roleName = roles.First();
        var currentRole = Roles.List.FirstOrDefault(role => role.Name == roleName);
        return currentRole!.Priority >= targetRole.Priority;
    }

    public async Task<bool> HasPermission(User user, int priority)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Count < 1) return false;
        var roleName = roles.First();
        var currentRole = Roles.List.FirstOrDefault(role => role.Name == roleName);
        return currentRole!.Priority >= priority;
    }

    [GeneratedRegex("\\[PZUser:[0-9]+:")]
    private static partial Regex UserRegex();
}