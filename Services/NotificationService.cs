using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class NotificationService(INotificationRepository notificationRepository, ITemplateService templateService,
        UserManager<User> userManager, IUserRelationRepository userRelationRepository, IResourceService resourceService)
    : INotificationService
{
    public async Task Notify(User receiver, User? sender, NotificationType type, string key,
        Dictionary<string, string> replacements)
    {
        var notification = new Notification
        {
            Type = type,
            Content = templateService.ReplacePlaceholders(templateService.GetMessage(key, receiver.Language)!,
                replacements),
            OwnerId = receiver.Id,
            OperatorId = sender?.Id,
            DateCreated = DateTimeOffset.UtcNow
        };
        await notificationRepository.CreateNotificationAsync(notification);
    }

    public async Task NotifyLike<T>(T resource, int userId, string display) where T : LikeableResource
    {
        var sender = (await userManager.FindByIdAsync(userId.ToString()))!;
        var relations = await userRelationRepository.GetRelationsAsync(["DateCreated"],
            [false], 0, -1,
            e => e.FolloweeId == userId && e.Type == UserRelationType.Special);
        var receivers = new HashSet<User> { (await userManager.FindByIdAsync(resource.OwnerId.ToString()))! };
        foreach (var relation in relations)
            receivers.Add((await userManager.FindByIdAsync(relation.FollowerId.ToString()))!);

        foreach (var receiver in receivers)
            await Notify(receiver, sender, NotificationType.Likes, "new-like",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(userId.ToString(), sender.UserName!) },
                    { "Resource", resourceService.GetRichText<T>(resource.Id.ToString(), display) }
                });
    }

    public async Task NotifyComment<T>(Comment comment, T resource, string display, string content)
        where T : LikeableResource
    {
        var sender = (await userManager.FindByIdAsync(comment.OwnerId.ToString()))!;
        var relations = await userRelationRepository.GetRelationsAsync(["DateCreated"],
            [false], 0, -1,
            e => e.FolloweeId == comment.OwnerId && e.Type == UserRelationType.Special);
        var receivers = new HashSet<User> { (await userManager.FindByIdAsync(resource.OwnerId.ToString()))! };
        foreach (var relation in relations)
            receivers.Add((await userManager.FindByIdAsync(relation.FollowerId.ToString()))!);

        foreach (var receiver in receivers.Where(receiver => receiver.Id != comment.OwnerId))
            await Notify(receiver, sender, NotificationType.Replies, "new-comment",
                new Dictionary<string, string>
                {
                    { "User", resourceService.GetRichText<User>(sender.Id.ToString(), sender.UserName!) },
                    { "Resource", resourceService.GetRichText<T>(resource.Id.ToString(), display) },
                    { "Comment", resourceService.GetRichText<Comment>(comment.Id.ToString(), content) }
                });
    }

    public async Task NotifyMentions(IEnumerable<User> users, User sender, string richText)
    {
        foreach (var user in users.Where(user => user.Id != sender.Id))
            await Notify(user, sender, NotificationType.Mentions, "mention",
                new Dictionary<string, string>
                {
                    {
                        "User", resourceService.GetRichText<User>(sender.Id.ToString(), sender.UserName!)
                    },
                    { "Content", richText }
                });
    }
}