using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    Task<ICollection<User>> GetUsersAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<User, bool>>? predicate = null);

    Task<User?> GetUserByTapUnionId(Guid applicationId, string unionId);

    Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null);
}