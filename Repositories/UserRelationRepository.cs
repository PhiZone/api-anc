using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class UserRelationRepository : IUserRelationRepository
{
    private readonly ApplicationDbContext _context;

    public UserRelationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<UserRelation>> GetFollowersAsync(int userId, string order, bool desc, int position,
        int take, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = _context.UserRelations.Where(relation => relation.FolloweeId == userId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, string order, bool desc, int position,
        int take, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = _context.UserRelations.Where(relation => relation.FollowerId == userId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetRelationsAsync(string order, bool desc, int position, int take,
        Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = _context.UserRelations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<UserRelation> GetRelationAsync(int followerId, int followeeId)
    {
        return (await _context.UserRelations.FirstOrDefaultAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId))!;
    }

    public async Task<bool> CreateRelationAsync(UserRelation userRelation)
    {
        await _context.UserRelations.AddAsync(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRelationAsync(int followerId, int followeeId)
    {
        _context.UserRelations.Remove((await _context.UserRelations.FirstOrDefaultAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRelationsAsync(Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null) return await _context.UserRelations.Where(predicate).CountAsync();
        return await _context.UserRelations.CountAsync();
    }

    public async Task<bool> RelationExistsAsync(int followerId, int followeeId)
    {
        return await _context.UserRelations.AnyAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId);
    }

    public async Task<int> CountFollowersAsync(User user, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.UserRelations.Where(relation => relation.Follower == user)
                .Where(predicate)
                .CountAsync();

        return await _context.UserRelations.Where(relation => relation.Followee == user).CountAsync();
    }

    public async Task<int> CountFolloweesAsync(User user, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.UserRelations.Where(relation => relation.Follower == user)
                .Where(predicate)
                .CountAsync();

        return await _context.UserRelations.Where(relation => relation.Follower == user).CountAsync();
    }
}