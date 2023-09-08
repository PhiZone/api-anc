using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface INotificationService
{
    Task Notify(User receiver, User? sender, NotificationType type, string key,
        Dictionary<string, string> replacements);

    Task NotifyLike<T>(T resource, int userId, string display) where T : LikeableResource;

    Task NotifyComment<T>(Comment comment, T resource, string display, string content) where T : LikeableResource;

    Task NotifyMentions(IEnumerable<User> users, User sender, string richText);
}