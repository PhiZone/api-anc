using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class EventResourceRepository(ApplicationDbContext context) : IEventResourceRepository
{
    public async Task<ICollection<EventResource>> GetDivisionsAsync(Guid resourceId, List<string>? order,
        List<bool>? desc, int? position = 0, int? take = -1, Expression<Func<EventResource, bool>>? predicate = null)
    {
        var result = context.EventResources.Where(eventResource => eventResource.ResourceId == resourceId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<EventResource>> GetResourcesAsync(Guid divisionId, List<string>? order,
        List<bool>? desc, int? position = 0, int? take = -1, Expression<Func<EventResource, bool>>? predicate = null)
    {
        var result = context.EventResources.Where(eventResource => eventResource.DivisionId == divisionId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<EventResource>> GetEventResourcesAsync(List<string>? order = null,
        List<bool>? desc = null, int? position = 0, int? take = -1,
        Expression<Func<EventResource, bool>>? predicate = null)
    {
        var result = context.EventResources.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<EventResource> GetEventResourceAsync(Guid divisionId, Guid resourceId)
    {
        return (await context.EventResources.FirstOrDefaultAsync(eventResource =>
            eventResource.DivisionId == divisionId && eventResource.ResourceId == resourceId))!;
    }

    public async Task<bool> CreateEventResourceAsync(EventResource eventResource)
    {
        await context.EventResources.AddAsync(eventResource);
        return await SaveAsync();
    }

    public async Task<bool> UpdateEventResourceAsync(EventResource eventResource)
    {
        context.EventResources.Update(eventResource);
        return await SaveAsync();
    }

    public async Task<bool> RemoveEventResourceAsync(Guid divisionId, Guid resourceId)
    {
        context.EventResources.Remove((await context.EventResources.FirstOrDefaultAsync(eventResource =>
            eventResource.DivisionId == divisionId && eventResource.ResourceId == resourceId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountEventResourcesAsync(Expression<Func<EventResource, bool>>? predicate = null)
    {
        if (predicate != null) return await context.EventResources.Where(predicate).CountAsync();
        return await context.EventResources.CountAsync();
    }

    public async Task<bool> EventResourceExistsAsync(Guid divisionId, Guid resourceId)
    {
        return await context.EventResources.AnyAsync(eventResource =>
            eventResource.DivisionId == divisionId && eventResource.ResourceId == resourceId);
    }

    public async Task<int> CountDivisionsAsync(Guid resourceId, Expression<Func<EventResource, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.EventResources.Where(eventResource => eventResource.ResourceId == resourceId)
                .Where(predicate)
                .CountAsync();

        return await context.EventResources.Where(eventResource => eventResource.ResourceId == resourceId).CountAsync();
    }

    public async Task<int> CountResourcesAsync(Guid divisionId, Expression<Func<EventResource, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.EventResources.Where(eventResource => eventResource.DivisionId == divisionId)
                .Where(predicate)
                .CountAsync();

        return await context.EventResources.Where(eventResource => eventResource.DivisionId == divisionId).CountAsync();
    }
}