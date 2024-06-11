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
    public async Task<ICollection<EventDivision>> GetEventDivisionsAsync(List<string>? order = null,
        List<bool>? desc = null, int? position = 0, int? take = -1,
        Expression<Func<EventDivision, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.EventDivisions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1))
                .Include(e => e.Teams.Where(team => team.Participants.Any(f => f.Id == currentUserId)).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<EventDivision> GetEventDivisionAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<EventDivision> result = context.EventDivisions;
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
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