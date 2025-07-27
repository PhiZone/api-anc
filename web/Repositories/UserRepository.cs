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
    public async Task<ICollection<User>> GetUsersAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<User, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Users.Include(e => e.Region).OrderBy(order, desc).Where(user => user.Id > 0);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<User?> GetUserByIdAsync(int id, int? currentUserId = null)
    {
        IQueryable<User> result = context.Users.Include(e => e.Region)
            .Include(e => e.ApplicationLinks.Where(link => link.RemoteUserId != null))
            .ThenInclude(e => e.Application)
            .Include(e => e.Hostships);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        return await result.FirstOrDefaultAsync(user => user.Id > 0 && user.Id == id);
    }

    public async Task<User?> GetUserByRemoteIdAsync(Guid applicationId, string remoteId, int? currentUserId = null)
    {
        var result = context.Users.Include(e => e.Region)
            .Include(e => e.ApplicationLinks.Where(link => link.RemoteUserId != null))
            .ThenInclude(e => e.Application)
            .Where(user => user.Id > 0);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        return await result.FirstOrDefaultAsync(user => user.ApplicationLinks.Any(relation =>
            relation.ApplicationId == applicationId && relation.RemoteUserId == remoteId));
    }

    public async Task<User?> GetUserByTapUnionIdAsync(Guid applicationId, string unionId, int? currentUserId = null)
    {
        var result = context.Users.Include(e => e.Region)
            .Include(e => e.ApplicationLinks.Where(link => link.RemoteUserId != null))
            .ThenInclude(e => e.Application)
            .Where(user => user.Id > 0);
        if (currentUserId != null)
            result = result.Include(e => e.FollowerRelations.Where(relation =>
                    relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                .Take(1));
        return await result.FirstOrDefaultAsync(user =>
            user.ApplicationLinks.Any(relation =>
                relation.ApplicationId == applicationId && relation.TapUnionId == unionId));
    }

    public async Task<int> CountUsersAsync(Expression<Func<User, bool>>? predicate = null)
    {
        var result = context.Users.Where(user => user.Id > 0);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }
}