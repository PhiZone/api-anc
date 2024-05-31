using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventDivisionRepository
{
    Task<ICollection<EventDivision>> GetEventDivisionsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<EventDivision, bool>>? predicate = null, int? currentUserId = null);

    Task<EventDivision> GetEventDivisionAsync(Guid id, int? currentUserId = null);

    Task<bool> EventDivisionExistsAsync(Guid id);

    Task<bool> CreateEventDivisionAsync(EventDivision eventDivision);

    Task<bool> UpdateEventDivisionAsync(EventDivision eventDivision);

    Task<bool> RemoveEventDivisionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountEventDivisionsAsync(Expression<Func<EventDivision, bool>>? predicate = null);
}