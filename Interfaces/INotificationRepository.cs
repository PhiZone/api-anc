using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface INotificationRepository
{
    Task<ICollection<Notification>> GetNotificationsAsync(string order, bool desc, int position, int take,
        string? search = null,
        Expression<Func<Notification, bool>>? predicate = null);

    Task<Notification> GetNotificationAsync(Guid id);

    Task<bool> NotificationExistsAsync(Guid id);

    Task<bool> CreateNotificationAsync(Notification notification);

    Task<bool> RemoveNotificationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountNotificationsAsync(string? search = null, Expression<Func<Notification, bool>>? predicate = null);
}