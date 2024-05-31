using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventRepository
{
    Task<ICollection<Event>> GetEventsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Event, bool>>? predicate = null, int? currentUserId = null);

    Task<Event> GetEventAsync(Guid id, int? currentUserId = null);

    Task<bool> EventExistsAsync(Guid id);

    Task<bool> CreateEventAsync(Event eventEntity);

    Task<bool> UpdateEventAsync(Event eventEntity);

    Task<bool> RemoveEventAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountEventsAsync(Expression<Func<Event, bool>>? predicate = null);
}