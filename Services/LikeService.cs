using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class LikeService(ILikeRepository likeRepository, IChapterRepository chapterRepository,
    ICollectionRepository collectionRepository, ISongRepository songRepository, IChartRepository chartRepository,
    IRecordRepository recordRepository, ICommentRepository commentRepository, IReplyRepository replyRepository,
    IApplicationRepository applicationRepository, IAnnouncementRepository announcementRepository,
    IResourceService resourceService, INotificationService notificationService) : ILikeService
{
    public async Task<bool> CreateLikeAsync(Chapter chapter, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(chapter.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = chapter.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(chapter, userId, chapter.GetDisplay());
        chapter.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == chapter.Id);
        return result && await chapterRepository.UpdateChapterAsync(chapter);
    }

    public async Task<bool> CreateLikeAsync(Collection collection, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(collection.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = collection.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(collection, userId, collection.GetDisplay());
        collection.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == collection.Id);
        return result && await collectionRepository.UpdateCollectionAsync(collection);
    }

    public async Task<bool> CreateLikeAsync(Song song, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(song.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = song.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(song, userId, song.GetDisplay());
        song.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == song.Id);
        return result && await songRepository.UpdateSongAsync(song);
    }

    public async Task<bool> CreateLikeAsync(Chart chart, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(chart.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = chart.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(chart, userId, await resourceService.GetDisplayName(chart));
        chart.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == chart.Id);
        return result && await chartRepository.UpdateChartAsync(chart);
    }

    public async Task<bool> CreateLikeAsync(Record record, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(record.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = record.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(record, userId, await resourceService.GetDisplayName(record));
        record.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == record.Id);
        return result && await recordRepository.UpdateRecordAsync(record);
    }

    public async Task<bool> CreateLikeAsync(Comment comment, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(comment.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = comment.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(comment, userId, comment.GetDisplay());
        comment.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == comment.Id);
        return result && await commentRepository.UpdateCommentAsync(comment);
    }

    public async Task<bool> CreateLikeAsync(Reply reply, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await likeRepository.LikeExistsAsync(reply.Id, userId)) return false;
        var like = new Like
        {
            ResourceId = reply.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow
        };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(reply, userId, reply.GetDisplay());
        reply.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == reply.Id);
        return result && await replyRepository.UpdateReplyAsync(reply);
    }

    public async Task<bool> CreateLikeAsync(Application application, int userId)
    {
        if (await likeRepository.LikeExistsAsync(application.Id, userId)) return false;
        var like = new Like { ResourceId = application.Id, OwnerId = userId, DateCreated = DateTimeOffset.UtcNow };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(application, userId, application.GetDisplay());
        application.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == application.Id);
        return result && await applicationRepository.UpdateApplicationAsync(application);
    }

    public async Task<bool> CreateLikeAsync(Announcement announcement, int userId)
    {
        if (await likeRepository.LikeExistsAsync(announcement.Id, userId)) return false;
        var like = new Like { ResourceId = announcement.Id, OwnerId = userId, DateCreated = DateTimeOffset.UtcNow };
        var result = await likeRepository.CreateLikeAsync(like);
        await notificationService.NotifyLike(announcement, userId, announcement.GetDisplay());
        announcement.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == announcement.Id);
        return result && await announcementRepository.UpdateAnnouncementAsync(announcement);
    }

    public async Task<bool> RemoveLikeAsync(Chapter chapter, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(chapter.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(chapter.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        chapter.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == chapter.Id);
        return result && await chapterRepository.UpdateChapterAsync(chapter);
    }

    public async Task<bool> RemoveLikeAsync(Collection collection, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(collection.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(collection.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        collection.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == collection.Id);
        return result && await collectionRepository.UpdateCollectionAsync(collection);
    }

    public async Task<bool> RemoveLikeAsync(Song song, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(song.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(song.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        song.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == song.Id);
        return result && await songRepository.UpdateSongAsync(song);
    }

    public async Task<bool> RemoveLikeAsync(Chart chart, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(chart.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(chart.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        chart.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == chart.Id);
        return result && await chartRepository.UpdateChartAsync(chart);
    }

    public async Task<bool> RemoveLikeAsync(Record record, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(record.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(record.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        record.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == record.Id);
        return result && await recordRepository.UpdateRecordAsync(record);
    }

    public async Task<bool> RemoveLikeAsync(Comment comment, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(comment.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(comment.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        comment.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == comment.Id);
        return result && await commentRepository.UpdateCommentAsync(comment);
    }

    public async Task<bool> RemoveLikeAsync(Reply reply, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(reply.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(reply.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        reply.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == reply.Id);
        return result && await replyRepository.UpdateReplyAsync(reply);
    }

    public async Task<bool> RemoveLikeAsync(Application application, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(application.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(application.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        application.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == application.Id);
        return result && await applicationRepository.UpdateApplicationAsync(application);
    }

    public async Task<bool> RemoveLikeAsync(Announcement announcement, int userId)
    {
        if (!await likeRepository.LikeExistsAsync(announcement.Id, userId)) return false;
        var like = await likeRepository.GetLikeAsync(announcement.Id, userId);
        var result = await likeRepository.RemoveLikeAsync(like.Id);
        announcement.LikeCount = await likeRepository.CountLikesAsync(e => e.ResourceId == announcement.Id);
        return result && await announcementRepository.UpdateAnnouncementAsync(announcement);
    }
}