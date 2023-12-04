using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class TapUserRelationRepository(ApplicationDbContext context) : ITapUserRelationRepository
{
    public async Task<ICollection<TapUserRelation>> GetApplicationsAsync(int userId, List<string> order,
        List<bool> desc,
        int position,
        int take, Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        var result = context.TapUserRelations
            .Where(relation => relation.UserId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<TapUserRelation>> GetUsersAsync(Guid applicationId, List<string> order,
        List<bool> desc,
        int position,
        int take, Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        var result = context.TapUserRelations
            .Where(relation => relation.ApplicationId == applicationId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<TapUserRelation>> GetRelationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        var result = context.TapUserRelations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<TapUserRelation> GetRelationAsync(Guid applicationId, int userId)
    {
        return (await context.TapUserRelations.FirstOrDefaultAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId))!;
    }

    public async Task<bool> CreateRelationAsync(TapUserRelation userRelation)
    {
        await context.TapUserRelations.AddAsync(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> UpdateRelationAsync(TapUserRelation userRelation)
    {
        context.TapUserRelations.Update(userRelation);
        return await SaveAsync();
    }

    public async Task<bool> RemoveRelationAsync(Guid applicationId, int userId)
    {
        context.TapUserRelations.Remove((await context.TapUserRelations.FirstOrDefaultAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRelationsAsync(Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        if (predicate != null) return await context.TapUserRelations.Where(predicate).CountAsync();
        return await context.TapUserRelations.CountAsync();
    }

    public async Task<bool> RelationExistsAsync(Guid applicationId, int userId)
    {
        return await context.TapUserRelations.AnyAsync(relation =>
            relation.ApplicationId == applicationId && relation.UserId == userId);
    }

    public async Task<int> CountApplicationsAsync(int userId, Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.TapUserRelations.Where(relation =>
                    relation.User.Id == userId)
                .Where(predicate)
                .CountAsync();

        return await context.TapUserRelations
            .Where(relation => relation.User.Id == userId)
            .CountAsync();
    }

    public async Task<int> CountUsersAsync(Guid applicationId,
        Expression<Func<TapUserRelation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.TapUserRelations.Where(relation =>
                    relation.Application.Id == applicationId)
                .Where(predicate)
                .CountAsync();

        return await context.TapUserRelations
            .Where(relation => relation.Application.Id == applicationId)
            .CountAsync();
    }
}