using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public async Task<ICollection<User>> GetUsersAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<User, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Users.Include(e => e.Region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id, int? currentUserId = null)
    {
        IQueryable<User> result = context.Users.Include(e => e.Region);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        return await result.FirstOrDefaultAsync(user => user.Id == id);
    }

    public async Task<User?> GetUserByTapUnionIdAsync(Guid applicationId, string unionId, int? currentUserId = null)
    {
        IQueryable<User> result = context.Users.Include(e => e.Region);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        return await result.FirstOrDefaultAsync(user =>
            user.TapUserRelations.Any(
                relation => relation.ApplicationId == applicationId && relation.UnionId == unionId));
    }

    public async Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null)
    {
        var result = context.Users.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}