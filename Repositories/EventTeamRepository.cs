using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class EventTeamRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IEventTeamRepository
{
    public async Task<ICollection<EventTeam>> GetEventTeamsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<EventTeam, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.EventTeams.Include(e => e.Participations)
            .ThenInclude(e => e.Participant)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<EventTeam> GetEventTeamAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<EventTeam> result = context.EventTeams.Include(e => e.Participations)
            .ThenInclude(e => e.Participant);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(eventTeam => eventTeam.Id == id))!;
    }

    public async Task<bool> EventTeamExistsAsync(Guid id)
    {
        return await context.EventTeams.AnyAsync(eventTeam => eventTeam.Id == id);
    }

    public async Task<bool> CreateEventTeamAsync(EventTeam eventTeam)
    {
        await context.EventTeams.AddAsync(eventTeam);
        await meilisearchService.AddAsync(eventTeam);
        return await SaveAsync();
    }

    public async Task<bool> UpdateEventTeamAsync(EventTeam eventTeam)
    {
        context.EventTeams.Update(eventTeam);
        await meilisearchService.UpdateAsync(eventTeam);
        return await SaveAsync();
    }

    public async Task<bool> RemoveEventTeamAsync(Guid id)
    {
        context.EventTeams.Remove((await context.EventTeams.FirstOrDefaultAsync(eventTeam => eventTeam.Id == id))!);
        await meilisearchService.DeleteAsync<EventTeam>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountEventTeamsAsync(Expression<Func<EventTeam, bool>>? predicate = null)
    {
        var result = context.EventTeams.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}