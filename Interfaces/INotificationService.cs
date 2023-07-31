using PhiZoneApi.Enums;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface INotificationService
{
    Task Notify(User receiver, User sender, NotificationType type, string key, Dictionary<string, string> replacements);
}