using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IResourceService _resourceService;
    private readonly ITemplateService _templateService;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public NotificationService(INotificationRepository notificationRepository, ITemplateService templateService,
        UserManager<User> userManager, IUserRelationRepository userRelationRepository, IResourceService resourceService)
    {
        _notificationRepository = notificationRepository;
        _templateService = templateService;
        _userManager = userManager;
        _userRelationRepository = userRelationRepository;
        _resourceService = resourceService;
    }

    public async Task Notify(User receiver, User? sender, NotificationType type, string key,
        Dictionary<string, string> replacements)
    {
        var notification = new Notification
        {
            Type = type,
            Content = _templateService.ReplacePlaceholders(_templateService.GetMessage(key, receiver.Language)!,
                replacements),
            OwnerId = receiver.Id,
            OperatorId = sender?.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await _notificationRepository.CreateNotificationAsync(notification);
    }

    public async Task NotifyLike<T>(T resource, int userId, string display) where T : LikeableResource
    {
        var sender = (await _userManager.FindByIdAsync(userId.ToString()))!;
        var relations = await _userRelationRepository.GetRelationsAsync(new List<string> { "DateCreated" },
            new List<bool> { false }, 0, -1,
            e => e.FolloweeId == userId && e.Type == UserRelationType.Special);
        var receivers = new HashSet<User> { (await _userManager.FindByIdAsync(resource.OwnerId.ToString()))! };
        foreach (var relation in relations)
            receivers.Add((await _userManager.FindByIdAsync(relation.FollowerId.ToString()))!);

        foreach (var receiver in receivers)
            await Notify(receiver, sender, NotificationType.Likes, "new-like",
                new Dictionary<string, string>
                {
                    { "User", _resourceService.GetRichText<User>(userId.ToString(), sender.UserName!) },
                    { "Resource", _resourceService.GetRichText<T>(resource.Id.ToString(), display) }
                });
    }

    public async Task NotifyComment<T>(Comment comment, T resource, string display, string content)
        where T : LikeableResource
    {
        var sender = (await _userManager.FindByIdAsync(comment.OwnerId.ToString()))!;
        var relations = await _userRelationRepository.GetRelationsAsync(new List<string> { "DateCreated" },
            new List<bool> { false }, 0, -1,
            e => e.FolloweeId == comment.OwnerId && e.Type == UserRelationType.Special);
        var receivers = new HashSet<User> { (await _userManager.FindByIdAsync(resource.OwnerId.ToString()))! };
        foreach (var relation in relations)
            receivers.Add((await _userManager.FindByIdAsync(relation.FollowerId.ToString()))!);

        foreach (var receiver in receivers.Where(receiver => receiver.Id != comment.OwnerId))
            await Notify(receiver, sender, NotificationType.Replies, "new-comment",
                new Dictionary<string, string>
                {
                    { "User", _resourceService.GetRichText<User>(sender.Id.ToString(), sender.UserName!) },
                    { "Resource", _resourceService.GetRichText<T>(resource.Id.ToString(), display) },
                    { "Comment", _resourceService.GetRichText<Comment>(comment.Id.ToString(), content) }
                });
    }

    public async Task NotifyMentions(IEnumerable<User> users, User sender, string richText)
    {
        foreach (var user in users.Where(user => user.Id != sender.Id))
            await Notify(user, sender, NotificationType.Mentions, "mention",
                new Dictionary<string, string>
                {
                    {
                        "User", _resourceService.GetRichText<User>(sender.Id.ToString(), sender.UserName!)
                    },
                    { "Content", richText }
                });
    }
}