using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Notification>> GetNotificationsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Notification, bool>>? predicate = null)
    {
        var result = _context.Notifications.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(notification => notification.Content.ToUpper().Contains(search));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Notification> GetNotificationAsync(Guid id)
    {
        return (await _context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id))!;
    }

    public async Task<bool> NotificationExistsAsync(Guid id)
    {
        return await _context.Notifications.AnyAsync(notification => notification.Id == id);
    }

    public async Task<bool> CreateNotificationAsync(Notification notification)
    {
        await _context.Notifications.AddAsync(notification);
        return await SaveAsync();
    }

    public async Task<bool> RemoveNotificationAsync(Guid id)
    {
        _context.Notifications.Remove(
            (await _context.Notifications.FirstOrDefaultAsync(notification => notification.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountNotificationsAsync(string? search = null,
        Expression<Func<Notification, bool>>? predicate = null)
    {
        var result = _context.Notifications.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(notification => notification.Content.ToUpper().Contains(search));
        }

        return await result.CountAsync();
    }
}