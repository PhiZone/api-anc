using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IApplicationRepository
{
    Task<ICollection<Application>> GetApplicationsAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<Application, bool>>? predicate = null);

    Task<Application> GetApplicationAsync(Guid id);

    Task<bool> ApplicationExistsAsync(Guid id);

    Task<bool> CreateApplicationAsync(Application application);

    Task<bool> UpdateApplicationAsync(Application application);

    Task<bool> RemoveApplicationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountApplicationsAsync(string? search = null, Expression<Func<Application, bool>>? predicate = null);
}