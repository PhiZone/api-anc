using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IApplicationServiceRepository
{
    Task<ICollection<ApplicationService>> GetApplicationServicesAsync(List<string> order, List<bool> desc, int position,
        int take, Expression<Func<ApplicationService, bool>>? predicate = null);

    Task<ApplicationService> GetApplicationServiceAsync(Guid id);

    Task<bool> ApplicationServiceExistsAsync(Guid id);

    Task<bool> CreateApplicationServiceAsync(ApplicationService applicationService);

    Task<bool> UpdateApplicationServiceAsync(ApplicationService applicationService);

    Task<bool> RemoveApplicationServiceAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountApplicationServicesAsync(Expression<Func<ApplicationService, bool>>? predicate = null);
}