using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class NotificationRepository(ApplicationDbContext context) : INotificationRepository
{
    public async Task<ICollection<Notification>> GetNotificationsAsync(List<string> order, List<bool> desc,
        int position, int take,
        string? search = null, Expression<Func<Notification, bool>>? predicate = null)
    {
        var result = context.Notifications.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(notification => EF.Functions.Like(notification.Content.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Notification> GetNotificationAsync(Guid id)
    {
        return (await context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id))!;
    }

    public async Task<bool> NotificationExistsAsync(Guid id)
    {
        return await context.Notifications.AnyAsync(notification => notification.Id == id);
    }

    public async Task<bool> CreateNotificationAsync(Notification notification)
    {
        await context.Notifications.AddAsync(notification);
        return await SaveAsync();
    }

    public async Task<bool> UpdateNotificationAsync(Notification notification)
    {
        context.Notifications.Update(notification);
        return await SaveAsync();
    }

    public async Task<bool> UpdateNotificationsAsync(IEnumerable<Notification> notifications)
    {
        context.Notifications.UpdateRange(notifications);
        return await SaveAsync();
    }

    public async Task<bool> RemoveNotificationAsync(Guid id)
    {
        context.Notifications.Remove(
            (await context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountNotificationsAsync(string? search = null,
        Expression<Func<Notification, bool>>? predicate = null)
    {
        var result = context.Notifications.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(notification => EF.Functions.Like(notification.Content.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}