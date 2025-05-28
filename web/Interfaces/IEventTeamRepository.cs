using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IEventTeamRepository
{
    Task<ICollection<EventTeam>> GetEventTeamsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<EventTeam, bool>>? predicate = null, int? currentUserId = null);

    Task<EventTeam> GetEventTeamAsync(Guid id, int? currentUserId = null);

    Task<bool> EventTeamExistsAsync(Guid id);

    Task<bool> CreateEventTeamAsync(EventTeam eventTeam);

    Task<bool> UpdateEventTeamAsync(EventTeam eventTeam);

    Task<bool> RemoveEventTeamAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountEventTeamsAsync(Expression<Func<EventTeam, bool>>? predicate = null);
}