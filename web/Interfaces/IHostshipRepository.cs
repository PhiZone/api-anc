using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IHostshipRepository
{
    Task<ICollection<Hostship>> GetEventsAsync(int userId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Hostship, bool>>? predicate = null);

    Task<ICollection<Hostship>> GetUsersAsync(Guid eventId, List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Hostship, bool>>? predicate = null);

    Task<ICollection<Hostship>> GetHostshipsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<Hostship, bool>>? predicate = null);

    Task<Hostship> GetHostshipAsync(Guid eventId, int userId);

    Task<bool> CreateHostshipAsync(Hostship hostship);

    Task<bool> UpdateHostshipAsync(Hostship hostship);

    Task<bool> RemoveHostshipAsync(Guid eventId, int userId);

    Task<bool> SaveAsync();

    Task<int> CountHostshipsAsync(Expression<Func<Hostship, bool>>? predicate = null);

    Task<bool> HostshipExistsAsync(Guid eventId, int userId);

    Task<int> CountEventsAsync(int userId, Expression<Func<Hostship, bool>>? predicate = null);

    Task<int> CountUsersAsync(Guid eventId, Expression<Func<Hostship, bool>>? predicate = null);
}