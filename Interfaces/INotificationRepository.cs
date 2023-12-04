using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface INotificationRepository
{
    Task<ICollection<Notification>> GetNotificationsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Notification, bool>>? predicate = null);

    Task<Notification> GetNotificationAsync(Guid id);

    Task<bool> NotificationExistsAsync(Guid id);

    Task<bool> CreateNotificationAsync(Notification notification);

    Task<bool> UpdateNotificationAsync(Notification notification);

    Task<bool> UpdateNotificationsAsync(IEnumerable<Notification> notifications);

    Task<bool> RemoveNotificationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountNotificationsAsync(Expression<Func<Notification, bool>>? predicate = null);
}