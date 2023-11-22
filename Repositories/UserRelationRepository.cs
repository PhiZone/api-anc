using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class UserRelationRepository(ApplicationDbContext context) : IUserRelationRepository
{
    public async Task<ICollection<UserRelation>> GetFollowersAsync(int userId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = context.UserRelations
            .Where(relation => relation.Type != UserRelationType.Blacklisted && relation.FolloweeId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = context.UserRelations
            .Where(relation => relation.Type != UserRelationType.Blacklisted && relation.FollowerId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<UserRelation, bool>>? predicate = null)
    {
        var result = context.UserRelations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<UserRelation> GetRelationAsync(int followerId, int followeeId)
    {
        return (await context.UserRelations.FirstOrDefaultAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId))!;
    }

    public async Task<bool> CreateRelationAsync(UserRelation userRelation)
    {
        await context.UserRelations.AddAsync(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRelationAsync(UserRelation userRelation)
    {
        context.UserRelations.Update(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRelationAsync(int followerId, int followeeId)
    {
        context.UserRelations.Remove((await context.UserRelations.FirstOrDefaultAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRelationsAsync(Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null) return await context.UserRelations.Where(predicate).CountAsync();
        return await context.UserRelations.CountAsync();
    }

    public async Task<bool> RelationExistsAsync(int followerId, int followeeId)
    {
        return await context.UserRelations.AnyAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId);
    }

    public async Task<int> CountFollowersAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.UserRelations.Where(relation =>
                    relation.Type != UserRelationType.Blacklisted && relation.Followee.Id == userId)
                .Where(predicate)
                .CountAsync();

        return await context.UserRelations
            .Where(relation => relation.Type != UserRelationType.Blacklisted && relation.Followee.Id == userId)
            .CountAsync();
    }

    public async Task<int> CountFolloweesAsync(int userId, Expression<Func<UserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.UserRelations.Where(relation =>
                    relation.Type != UserRelationType.Blacklisted && relation.Follower.Id == userId)
                .Where(predicate)
                .CountAsync();

        return await context.UserRelations
            .Where(relation => relation.Type != UserRelationType.Blacklisted && relation.Follower.Id == userId)
            .CountAsync();
    }
}