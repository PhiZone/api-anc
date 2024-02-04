using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Models;

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

    bool HasPermission(User user, UserRole role);

    Task<(string, List<User>)> ParseUserContent(string content);

    string Normalize(string input);

    ResourceDto GetResources();
}