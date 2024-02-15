using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class EventTaskRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IEventTaskRepository
{
    public async Task<ICollection<EventTask>> GetEventTasksAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<EventTask, bool>>? predicate = null)
    {
        var result = context.EventTasks.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<EventTask> GetEventTaskAsync(Guid id)
    {
        return (await context.EventTasks.FirstOrDefaultAsync(eventTask => eventTask.Id == id))!;
    }

    public async Task<bool> EventTaskExistsAsync(Guid id)
    {
        return await context.EventTasks.AnyAsync(eventTask => eventTask.Id == id);
    }

    public async Task<bool> CreateEventTaskAsync(EventTask eventTask)
    {
        await context.EventTasks.AddAsync(eventTask);
        await meilisearchService.AddAsync(eventTask);
        return await SaveAsync();
    }

    public async Task<bool> UpdateEventTaskAsync(EventTask eventTask)
    {
        context.EventTasks.Update(eventTask);
        await meilisearchService.UpdateAsync(eventTask);
        return await SaveAsync();
    }

    public async Task<bool> RemoveEventTaskAsync(Guid id)
    {
        context.EventTasks.Remove(
            (await context.EventTasks.FirstOrDefaultAsync(eventTask => eventTask.Id == id))!);
        await meilisearchService.DeleteAsync<EventTask>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountEventTasksAsync(Expression<Func<EventTask, bool>>? predicate = null)
    {
        var result = context.EventTasks.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}