using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class HostshipRepository(ApplicationDbContext context) : IHostshipRepository
{
    public async Task<ICollection<Hostship>> GetEventsAsync(int userId, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Hostship, bool>>? predicate = null)
    {
        var result = context.Hostships.Where(admission => admission.UserId == userId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Hostship>> GetUsersAsync(Guid eventId, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Hostship, bool>>? predicate = null)
    {
        var result = context.Hostships.Where(admission => admission.EventId == eventId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Hostship>> GetHostshipsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<Hostship, bool>>? predicate = null)
    {
        var result = context.Hostships.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Hostship> GetHostshipAsync(Guid eventId, int userId)
    {
        return (await context.Hostships.FirstOrDefaultAsync(admission =>
            admission.EventId == eventId && admission.UserId == userId))!;
    }

    public async Task<bool> CreateHostshipAsync(Hostship admission)
    {
        await context.Hostships.AddAsync(admission);
        return await SaveAsync();
    }

    public async Task<bool> UpdateHostshipAsync(Hostship admission)
    {
        context.Hostships.Update(admission);
        return await SaveAsync();
    }

    public async Task<bool> RemoveHostshipAsync(Guid eventId, int userId)
    {
        context.Hostships.Remove((await context.Hostships.FirstOrDefaultAsync(admission =>
            admission.EventId == eventId && admission.UserId == userId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountHostshipsAsync(Expression<Func<Hostship, bool>>? predicate = null)
    {
        if (predicate != null) return await context.Hostships.Where(predicate).CountAsync();
        return await context.Hostships.CountAsync();
    }

    public async Task<bool> HostshipExistsAsync(Guid eventId, int userId)
    {
        return await context.Hostships.AnyAsync(admission =>
            admission.EventId == eventId && admission.UserId == userId);
    }

    public async Task<int> CountEventsAsync(int userId, Expression<Func<Hostship, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.Hostships.Where(admission => admission.UserId == userId)
                .Where(predicate)
                .CountAsync();

        return await context.Hostships.Where(admission => admission.UserId == userId).CountAsync();
    }

    public async Task<int> CountUsersAsync(Guid eventId, Expression<Func<Hostship, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.Hostships.Where(admission => admission.EventId == eventId)
                .Where(predicate)
                .CountAsync();

        return await context.Hostships.Where(admission => admission.EventId == eventId).CountAsync();
    }
}