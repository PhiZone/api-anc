using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class EventRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : IEventRepository
{
    public async Task<ICollection<Event>> GetEventsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<Event, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Events.Include(e => e.Divisions)
            .Include(e => e.Hostships)
            .ThenInclude(e => e.User).ThenInclude(e => e.Region)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Event> GetEventAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Event> result = context.Events.Include(e => e.Divisions)
            .Include(e => e.Hostships)
            .ThenInclude(e => e.User).ThenInclude(e => e.Region);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(eventEntity => eventEntity.Id == id))!;
    }

    public async Task<bool> EventExistsAsync(Guid id)
    {
        return await context.Events.AnyAsync(eventEntity => eventEntity.Id == id);
    }

    public async Task<bool> CreateEventAsync(Event eventEntity)
    {
        await context.Events.AddAsync(eventEntity);
        await meilisearchService.AddAsync(eventEntity);
        return await SaveAsync();
    }

    public async Task<bool> UpdateEventAsync(Event eventEntity)
    {
        context.Events.Update(eventEntity);
        await meilisearchService.UpdateAsync(eventEntity);
        return await SaveAsync();
    }

    public async Task<bool> RemoveEventAsync(Guid id)
    {
        context.Events.Remove((await context.Events.FirstOrDefaultAsync(eventEntity => eventEntity.Id == id))!);
        await meilisearchService.DeleteAsync<Event>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountEventsAsync(Expression<Func<Event, bool>>? predicate = null)
    {
        var result = context.Events.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}