using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IApplicationRepository
{
    Task<ICollection<Application>> GetApplicationsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<Application, bool>>? predicate = null, int? currentUserId = null);

    Task<Application> GetApplicationAsync(Guid id, int? currentUserId = null);

    Task<bool> ApplicationExistsAsync(Guid id);

    Task<bool> CreateApplicationAsync(Application application);

    Task<bool> UpdateApplicationAsync(Application application);

    Task<bool> RemoveApplicationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountApplicationsAsync(Expression<Func<Application, bool>>? predicate = null);
}