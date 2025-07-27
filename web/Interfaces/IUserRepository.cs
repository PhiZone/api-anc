using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    Task<ICollection<User>> GetUsersAsync(List<string>? order = null, List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<User, bool>>? predicate = null, int? currentUserId = null);

    Task<User?> GetUserByIdAsync(int id, int? currentUserId = null);

    Task<User?> GetUserByRemoteIdAsync(Guid applicationId, string remoteId, int? currentUserId = null);

    Task<User?> GetUserByTapUnionIdAsync(Guid applicationId, string unionId, int? currentUserId = null);

    Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null);

    Task<bool> SaveAsync();
}