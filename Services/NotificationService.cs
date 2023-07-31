using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ITemplateService _templateService;
    
    public NotificationService(INotificationRepository notificationRepository, ITemplateService templateService)
    {
        _notificationRepository = notificationRepository;
        _templateService = templateService;
    }

    public async Task Notify(User receiver, User sender, NotificationType type, string key, Dictionary<string, string> replacements)
    {
        var notification = new Notification
        {
            Type = type,
            Content = _templateService.ReplacePlaceholders(_templateService.GetMessage(key, receiver.Language)!, replacements),
            OwnerId = receiver.Id,
            OperatorId = sender.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await _notificationRepository.CreateNotificationAsync(notification);
    }
}