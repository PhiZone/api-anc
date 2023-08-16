using PhiZoneApi.Models;
using Role = PhiZoneApi.Constants.Role;

namespace PhiZoneApi.Interfaces;

public interface IResourceService
{
    string GetRichText<T>(string id, string display);

    string GetComplexRichText<T>(string id1, string id2, string display);

    List<int> GetAuthorIds(string name);

    Task<bool> HasPermission(User user, Role targetRole);

    Task<bool> HasPermission(User user, int priority);

    Task<Role?> GetRole(User user);
}