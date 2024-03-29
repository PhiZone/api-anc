using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class UserRelationRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IUserRelationRepository
{
    public async Task<ICollection<UserRelation>> GetFollowersAsync(int userId, List<string> order, List<bool> desc,
        int position, int take, Expression<Func<UserRelation, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.UserRelations.Include(e => e.Follower)
            .Where(e => e.Type != UserRelationType.Blacklisted && e.FolloweeId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Follower)
                .ThenInclude(e => e.FollowerRelations.Where(relation =>
                        relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                    .Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, List<string> order, List<bool> desc,
        int position, int take, Expression<Func<UserRelation, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.UserRelations.Include(e => e.Followee)
            .Where(relation => relation.Type != UserRelationType.Blacklisted && relation.FollowerId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Followee)
                .ThenInclude(e => e.FollowerRelations.Where(relation =>
                        relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                    .Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take, Expression<Func<UserRelation, bool>>? predicate = null)
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
        var follower = await context.Users.FirstAsync(e => e.Id == userRelation.FollowerId);
        var followee = await context.Users.FirstAsync(e => e.Id == userRelation.FolloweeId);
        follower.FolloweeCount = await context.UserRelations.LongCountAsync(e =>
            e.FollowerId == userRelation.FollowerId && e.Type != UserRelationType.Blacklisted) + 1;
        followee.FollowerCount = await context.UserRelations.LongCountAsync(e =>
            e.FolloweeId == userRelation.FolloweeId && e.Type != UserRelationType.Blacklisted) + 1;
        await context.UserRelations.AddAsync(userRelation);
        context.Users.UpdateRange(follower, followee);
        await meilisearchService.UpdateBatchAsync([follower, followee]);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRelationAsync(UserRelation userRelation)
    {
        var follower = await context.Users.FirstAsync(e => e.Id == userRelation.FollowerId);
        var followee = await context.Users.FirstAsync(e => e.Id == userRelation.FolloweeId);
        context.UserRelations.Update(userRelation);
        var result = await SaveAsync();
        follower.FolloweeCount = await context.UserRelations.LongCountAsync(e =>
            e.FollowerId == userRelation.FollowerId && e.Type != UserRelationType.Blacklisted);
        followee.FollowerCount = await context.UserRelations.LongCountAsync(e =>
            e.FolloweeId == userRelation.FolloweeId && e.Type != UserRelationType.Blacklisted);
        context.Users.UpdateRange(follower, followee);
        await meilisearchService.UpdateBatchAsync([follower, followee]);
        return result && await SaveAsync();
    }

    public async Task<bool> RemoveRelationAsync(int followerId, int followeeId)
    {
        var relation = await context.UserRelations.Include(e => e.Follower)
            .Include(e => e.Followee)
            .FirstAsync(relation => relation.FollowerId == followerId && relation.FolloweeId == followeeId);
        relation.Follower.FolloweeCount =
            await context.UserRelations.CountAsync(e =>
                e.FollowerId == followerId && e.Type != UserRelationType.Blacklisted) - 1;
        relation.Followee.FollowerCount =
            await context.UserRelations.CountAsync(e =>
                e.FolloweeId == followeeId && e.Type != UserRelationType.Blacklisted) - 1;
        context.UserRelations.Remove(relation);
        context.Users.UpdateRange(relation.Follower, relation.Followee);
        await meilisearchService.UpdateBatchAsync([relation.Follower, relation.Followee]);
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