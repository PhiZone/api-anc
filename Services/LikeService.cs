using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class LikeService : ILikeService
{
    private readonly IAnnouncementRepository _announcementRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly IChapterRepository _chapterRepository;
    private readonly IChartRepository _chartRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly IRecordRepository _recordRepository;
    private readonly IReplyRepository _replyRepository;
    private readonly ISongRepository _songRepository;

    public LikeService(ILikeRepository likeRepository, IChapterRepository chapterRepository,
        ISongRepository songRepository, IChartRepository chartRepository, IRecordRepository recordRepository,
        ICommentRepository commentRepository, IReplyRepository replyRepository,
        IApplicationRepository applicationRepository, IAnnouncementRepository announcementRepository)
    {
        _likeRepository = likeRepository;
        _chapterRepository = chapterRepository;
        _songRepository = songRepository;
        _chartRepository = chartRepository;
        _recordRepository = recordRepository;
        _commentRepository = commentRepository;
        _replyRepository = replyRepository;
        _applicationRepository = applicationRepository;
        _announcementRepository = announcementRepository;
    }

    public async Task<bool> CreateLikeAsync(Chapter chapter, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(chapter.Id, userId)) return false;
        var like = new Like { ResourceId = chapter.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        chapter.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == chapter.Id);
        return result && await _chapterRepository.UpdateChapterAsync(chapter);
    }

    public async Task<bool> CreateLikeAsync(Song song, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(song.Id, userId)) return false;
        var like = new Like { ResourceId = song.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        song.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == song.Id);
        return result && await _songRepository.UpdateSongAsync(song);
    }

    public async Task<bool> CreateLikeAsync(Chart chart, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(chart.Id, userId)) return false;
        var like = new Like { ResourceId = chart.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        chart.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == chart.Id);
        return result && await _chartRepository.UpdateChartAsync(chart);
    }

    public async Task<bool> CreateLikeAsync(Record record, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(record.Id, userId)) return false;
        var like = new Like { ResourceId = record.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        record.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == record.Id);
        return result && await _recordRepository.UpdateRecordAsync(record);
    }

    public async Task<bool> CreateLikeAsync(Comment comment, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(comment.Id, userId)) return false;
        var like = new Like { ResourceId = comment.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        comment.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == comment.Id);
        return result && await _commentRepository.UpdateCommentAsync(comment);
    }

    public async Task<bool> CreateLikeAsync(Reply reply, int userId, DateTimeOffset? dateCreated = null)
    {
        if (await _likeRepository.LikeExistsAsync(reply.Id, userId)) return false;
        var like = new Like { ResourceId = reply.Id, OwnerId = userId, DateCreated = dateCreated ?? DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        reply.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == reply.Id);
        return result && await _replyRepository.UpdateReplyAsync(reply);
    }

    public async Task<bool> CreateLikeAsync(Application application, int userId)
    {
        if (await _likeRepository.LikeExistsAsync(application.Id, userId)) return false;
        var like = new Like { ResourceId = application.Id, OwnerId = userId, DateCreated = DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        application.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == application.Id);
        return result && await _applicationRepository.UpdateApplicationAsync(application);
    }

    public async Task<bool> CreateLikeAsync(Announcement announcement, int userId)
    {
        if (await _likeRepository.LikeExistsAsync(announcement.Id, userId)) return false;
        var like = new Like { ResourceId = announcement.Id, OwnerId = userId, DateCreated = DateTimeOffset.UtcNow };
        var result = await _likeRepository.CreateLikeAsync(like);
        announcement.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == announcement.Id);
        return result && await _announcementRepository.UpdateAnnouncementAsync(announcement);
    }

    public async Task<bool> RemoveLikeAsync(Chapter chapter, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(chapter.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(chapter.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        chapter.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == chapter.Id);
        return result && await _chapterRepository.UpdateChapterAsync(chapter);
    }

    public async Task<bool> RemoveLikeAsync(Song song, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(song.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(song.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        song.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == song.Id);
        return result && await _songRepository.UpdateSongAsync(song);
    }

    public async Task<bool> RemoveLikeAsync(Chart chart, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(chart.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(chart.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        chart.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == chart.Id);
        return result && await _chartRepository.UpdateChartAsync(chart);
    }

    public async Task<bool> RemoveLikeAsync(Record record, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(record.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(record.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        record.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == record.Id);
        return result && await _recordRepository.UpdateRecordAsync(record);
    }

    public async Task<bool> RemoveLikeAsync(Comment comment, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(comment.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(comment.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        comment.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == comment.Id);
        return result && await _commentRepository.UpdateCommentAsync(comment);
    }

    public async Task<bool> RemoveLikeAsync(Reply reply, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(reply.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(reply.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        reply.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == reply.Id);
        return result && await _replyRepository.UpdateReplyAsync(reply);
    }

    public async Task<bool> RemoveLikeAsync(Application application, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(application.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(application.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        application.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == application.Id);
        return result && await _applicationRepository.UpdateApplicationAsync(application);
    }

    public async Task<bool> RemoveLikeAsync(Announcement announcement, int userId)
    {
        if (!await _likeRepository.LikeExistsAsync(announcement.Id, userId)) return false;
        var like = await _likeRepository.GetLikeAsync(announcement.Id, userId);
        var result = await _likeRepository.RemoveLikeAsync(like.Id);
        announcement.LikeCount = await _likeRepository.CountLikesAsync(e => e.ResourceId == announcement.Id);
        return result && await _announcementRepository.UpdateAnnouncementAsync(announcement);
    }
}