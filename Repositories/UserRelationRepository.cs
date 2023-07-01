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
        int take)
    {
        return await _context.UserRelations.Where(relation => relation.FolloweeId == userId)
            .OrderBy(order, desc)
            .Skip(position)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetFolloweesAsync(int userId, string order, bool desc, int position,
        int take)
    {
        return await _context.UserRelations.Where(relation => relation.FollowerId == userId)
            .OrderBy(order, desc)
            .Skip(position)
            .Take(take)
            .ToListAsync();
    }

    public async Task<ICollection<UserRelation>> GetRelationsAsync(string order, bool desc, int position, int take)
    {
        return await _context.UserRelations.OrderBy(order, desc).Skip(position).Take(take).ToListAsync();
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

    public async Task<bool> RemoveRelationAsync(UserRelation userRelation)
    {
        _context.UserRelations.Remove(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAsync()
    {
        return await _context.UserRelations.CountAsync();
    }

    public async Task<bool> RelationExistsAsync(int followerId, int followeeId)
    {
        return await _context.UserRelations.AnyAsync(relation =>
            relation.FollowerId == followerId && relation.FolloweeId == followeeId);
    }

    public async Task<int> CountFollowersAsync(User user)
    {
        return await _context.UserRelations.Where(relation => relation.Followee == user).CountAsync();
    }

    public async Task<int> CountFolloweesAsync(User user)
    {
        return await _context.UserRelations.Where(relation => relation.Follower == user).CountAsync();
    }
}