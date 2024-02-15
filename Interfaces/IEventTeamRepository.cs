using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventTeamRepository
{
    Task<ICollection<EventTeam>> GetEventTeamsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<EventTeam, bool>>? predicate = null);

    Task<EventTeam> GetEventTeamAsync(Guid id);

    Task<bool> EventTeamExistsAsync(Guid id);

    Task<bool> CreateEventTeamAsync(EventTeam eventTeam);

    Task<bool> UpdateEventTeamAsync(EventTeam eventTeam);

    Task<bool> RemoveEventTeamAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountEventTeamsAsync(Expression<Func<EventTeam, bool>>? predicate = null);
}