using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class NotificationRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : INotificationRepository
{
    public async Task<ICollection<Notification>> GetNotificationsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<Notification, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Notifications.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Operator).ThenInclude(e => e!.Region);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Notification> GetNotificationAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Notification> result = context.Notifications;
        if (currentUserId != null)
            result = result.Include(e => e.Operator).ThenInclude(e => e!.Region);
        return (await result.FirstOrDefaultAsync(notification => notification.Id == id))!;
    }

    public async Task<bool> NotificationExistsAsync(Guid id)
    {
        return await context.Notifications.AnyAsync(notification => notification.Id == id);
    }

    public async Task<bool> CreateNotificationAsync(Notification notification)
    {
        await context.Notifications.AddAsync(notification);
        await meilisearchService.AddAsync(notification);
        return await SaveAsync();
    }

    public async Task<bool> UpdateNotificationAsync(Notification notification)
    {
        context.Notifications.Update(notification);
        await meilisearchService.UpdateAsync(notification);
        return await SaveAsync();
    }

    public async Task<bool> UpdateNotificationsAsync(IEnumerable<Notification> notifications)
    {
        var enumerable = notifications.ToList();
        context.Notifications.UpdateRange(enumerable);
        await meilisearchService.UpdateBatchAsync(enumerable);
        return await SaveAsync();
    }

    public async Task<bool> RemoveNotificationAsync(Guid id)
    {
        context.Notifications.Remove(
            (await context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id))!);
        await meilisearchService.DeleteAsync<Notification>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountNotificationsAsync(Expression<Func<Notification, bool>>? predicate = null)
    {
        var result = context.Notifications.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}