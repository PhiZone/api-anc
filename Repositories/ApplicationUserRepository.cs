using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ApplicationUserRepository(ApplicationDbContext context) : IApplicationUserRepository
{
    public async Task<ICollection<ApplicationUser>> GetApplicationsAsync(int userId, List<string> order,
        List<bool> desc, int position, int take, Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        var result = context.ApplicationUsers.Where(relation => relation.UserId == userId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<ApplicationUser>> GetUsersAsync(Guid applicationId, List<string> order,
        List<bool> desc, int position, int take, Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        var result = context.ApplicationUsers.Where(relation => relation.ApplicationId == applicationId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<ApplicationUser>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take, Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        var result = context.ApplicationUsers.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ApplicationUser> GetRelationAsync(Guid applicationId, int userId)
    {
        return (await context.ApplicationUsers.FirstOrDefaultAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId))!;
    }

    public async Task<ApplicationUser> GetRelationAsync(Guid applicationId, string remoteUserId)
    {
        return (await context.ApplicationUsers.Include(e => e.User)
            .FirstOrDefaultAsync(relation =>
                relation.ApplicationId == applicationId && relation.RemoteUserId == remoteUserId))!;
    }

    public async Task<bool> CreateRelationAsync(ApplicationUser applicationUser)
    {
        await context.ApplicationUsers.AddAsync(applicationUser);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRelationAsync(ApplicationUser applicationUser)
    {
        context.ApplicationUsers.Update(applicationUser);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRelationAsync(Guid applicationId, int userId)
    {
        context.ApplicationUsers.Remove((await context.ApplicationUsers.FirstOrDefaultAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRelationsAsync(Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        if (predicate != null) return await context.ApplicationUsers.Where(predicate).CountAsync();
        return await context.ApplicationUsers.CountAsync();
    }

    public async Task<bool> RelationExistsAsync(Guid applicationId, int userId)
    {
        return await context.ApplicationUsers.AnyAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId);
    }

    public async Task<bool> RelationExistsAsync(Guid applicationId, string remoteUserId)
    {
        return await context.ApplicationUsers.AnyAsync(relation =>
            relation.ApplicationId == applicationId && relation.RemoteUserId == remoteUserId);
    }

    public async Task<int> CountApplicationsAsync(int userId, Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.ApplicationUsers.Where(relation => relation.User.Id == userId)
                .Where(predicate)
                .CountAsync();

        return await context.ApplicationUsers.Where(relation => relation.User.Id == userId).CountAsync();
    }

    public async Task<int> CountUsersAsync(Guid applicationId,
        Expression<Func<ApplicationUser, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.ApplicationUsers.Where(relation => relation.Application.Id == applicationId)
                .Where(predicate)
                .CountAsync();

        return await context.ApplicationUsers.Where(relation => relation.Application.Id == applicationId).CountAsync();
    }
}