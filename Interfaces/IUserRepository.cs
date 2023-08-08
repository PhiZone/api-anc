using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    Task<ICollection<User>> GetUsersAsync(string order, bool desc, int position, int take, string? search = null,
        Expression<Func<User, bool>>? predicate = null);

    Task<User?> GetUserByTapUnionId(string unionId);

    Task<int> CountUsersAsync(string? search = null, Expression<Func<User, bool>>? predicate = null);
}