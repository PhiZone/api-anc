using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public async Task<ICollection<User>> GetUsersAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<User, bool>>? predicate = null)
    {
        var result = context.Users.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<User?> GetUserByTapUnionId(Guid applicationId, string unionId)
    {
        return await context.Users.FirstOrDefaultAsync(user =>
            user.TapUserRelations.Any(relation =>
                relation.ApplicationId == applicationId && relation.UnionId == unionId));
    }

    public async Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null)
    {
        var result = context.Users.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}