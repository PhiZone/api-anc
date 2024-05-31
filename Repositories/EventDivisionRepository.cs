using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class EventDivisionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IEventDivisionRepository
{
    public async Task<ICollection<EventDivision>> GetEventDivisionsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<EventDivision, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.EventDivisions.Include(e => e.Administrators).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<EventDivision> GetEventDivisionAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<EventDivision> result = context.EventDivisions.Include(e => e.Administrators);
        return (await result.FirstOrDefaultAsync(eventDivision => eventDivision.Id == id))!;
    }

    public async Task<bool> EventDivisionExistsAsync(Guid id)
    {
        return await context.EventDivisions.AnyAsync(eventDivision => eventDivision.Id == id);
    }

    public async Task<bool> CreateEventDivisionAsync(EventDivision eventDivision)
    {
        await context.EventDivisions.AddAsync(eventDivision);
        await meilisearchService.AddAsync(eventDivision);
        return await SaveAsync();
    }

    public async Task<bool> UpdateEventDivisionAsync(EventDivision eventDivision)
    {
        context.EventDivisions.Update(eventDivision);
        await meilisearchService.UpdateAsync(eventDivision);
        return await SaveAsync();
    }

    public async Task<bool> RemoveEventDivisionAsync(Guid id)
    {
        context.EventDivisions.Remove(
            (await context.EventDivisions.FirstOrDefaultAsync(eventDivision => eventDivision.Id == id))!);
        await meilisearchService.DeleteAsync<EventDivision>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountEventDivisionsAsync(Expression<Func<EventDivision, bool>>? predicate = null)
    {
        var result = context.EventDivisions.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}