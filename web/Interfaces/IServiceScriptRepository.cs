using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IServiceScriptRepository
{
    Task<ICollection<ServiceScript>> GetServiceScriptsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<ServiceScript, bool>>? predicate = null, int? currentUserId = null);

    Task<ServiceScript> GetServiceScriptAsync(Guid id, int? currentUserId = null);

    Task<bool> ServiceScriptExistsAsync(Guid id);

    Task<bool> CreateServiceScriptAsync(ServiceScript serviceScript);

    Task<bool> UpdateServiceScriptAsync(ServiceScript serviceScript);

    Task<bool> RemoveServiceScriptAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountServiceScriptsAsync(Expression<Func<ServiceScript, bool>>? predicate = null);
}