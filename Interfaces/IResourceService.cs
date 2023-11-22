using PhiZoneApi.Models;
using Role = PhiZoneApi.Constants.Role;

namespace PhiZoneApi.Interfaces;

public interface IResourceService
{
    string GetRichText<T>(string id, string display, string? addition = null);

    string GetComplexRichText(string type, string id1, string id2, string display, string? addition = null);

    Task<string> GetDisplayName(Chart chart);

    Task<string> GetDisplayName(ChartSubmission chart);

    Task<string> GetDisplayName(Record record);

    List<int> GetAuthorIds(string name);

    Task<bool> IsBlacklisted(int user1, int user2);

    Task<bool> HasPermission(User user, Role targetRole);

    Task<bool> HasPermission(User user, int priority);

    Task<Role?> GetRole(User user);

    Task<(string, List<User>)> ParseUserContent(string content);
}