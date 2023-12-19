using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventTaskRepository
{
    Task<ICollection<EventTask>> GetEventTasksAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<EventTask, bool>>? predicate = null);

    Task<EventTask> GetEventTaskAsync(Guid id);

    Task<bool> EventTaskExistsAsync(Guid id);

    Task<bool> CreateEventTaskAsync(EventTask eventTask);

    Task<bool> UpdateEventTaskAsync(EventTask eventTask);

    Task<bool> RemoveEventTaskAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountEventTasksAsync(Expression<Func<EventTask, bool>>? predicate = null);
}