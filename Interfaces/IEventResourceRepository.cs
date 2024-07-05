using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventResourceRepository
{
    Task<ICollection<EventResource>> GetDivisionsAsync(Guid resourceId, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<EventResource, bool>>? predicate = null);

    Task<ICollection<EventResource>> GetResourcesAsync(Guid divisionId, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<EventResource, bool>>? predicate = null);

    Task<ICollection<EventResource>> GetEventResourcesAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<EventResource, bool>>? predicate = null);

    Task<EventResource> GetEventResourceAsync(Guid divisionId, Guid resourceId);

    Task<bool> CreateEventResourceAsync(EventResource eventResource);

    Task<bool> UpdateEventResourceAsync(EventResource eventResource);

    Task<bool> RemoveEventResourceAsync(Guid divisionId, Guid resourceId);

    Task<bool> SaveAsync();

    Task<int> CountEventResourcesAsync(Expression<Func<EventResource, bool>>? predicate = null);

    Task<bool> EventResourceExistsAsync(Guid divisionId, Guid resourceId);

    Task<int> CountDivisionsAsync(Guid resourceId, Expression<Func<EventResource, bool>>? predicate = null);

    Task<int> CountResourcesAsync(Guid divisionId, Expression<Func<EventResource, bool>>? predicate = null);
}