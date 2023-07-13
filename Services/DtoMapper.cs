using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class DtoMapper : IDtoMapper
{
    private readonly ICommentRepository _commentRepository;
    private readonly ILikeRepository _likeRepository;
    private readonly IMapper _mapper;
    private readonly IRecordRepository _recordRepository;
    private readonly IRegionRepository _regionRepository;
    private readonly IReplyRepository _replyRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public DtoMapper(IUserRelationRepository userRelationRepository, IRegionRepository regionRepository,
        ILikeRepository likeRepository, UserManager<User> userManager, IMapper mapper,
        ICommentRepository commentRepository, IReplyRepository replyRepository, IRecordRepository recordRepository)
    {
        _userRelationRepository = userRelationRepository;
        _regionRepository = regionRepository;
        _likeRepository = likeRepository;
        _userManager = userManager;
        _mapper = mapper;
        _commentRepository = commentRepository;
        _replyRepository = replyRepository;
        _recordRepository = recordRepository;
    }

    public async Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto
    {
        var dto = _mapper.Map<T>(user);
        dto.Roles = await _userManager.GetRolesAsync(user);
        dto.FollowerCount = await _userRelationRepository.CountFollowersAsync(user);
        dto.FolloweeCount = await _userRelationRepository.CountFolloweesAsync(user);
        if (user.RegionId != null) dto.Region = (await _regionRepository.GetRegionAsync((int)user.RegionId)).Code;
        if (currentUser != null && await _userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
            dto.DateFollowed = (await _userRelationRepository.GetRelationAsync(currentUser.Id, user.Id)).DateCreated;
        return dto;
    }

    public async Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await _userManager.FindByIdAsync(userRelation.FolloweeId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null)
        where T : UserRelationDto
    {
        var dto = _mapper.Map<T>(userRelation);
        dto.Follower = await MapFollowerAsync<UserDto>(userRelation, currentUser);
        dto.Followee = await MapFolloweeAsync<UserDto>(userRelation, currentUser);
        return dto;
    }

    public async Task<T> MapChapterAsync<T>(Chapter chapter, User? currentUser = null) where T : ChapterDto
    {
        var dto = _mapper.Map<T>(chapter);
        dto.CommentCount = await _commentRepository.CountCommentsAsync(comment => comment.ResourceId == chapter.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(chapter.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(chapter.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapSongAsync<T>(Song song, User? currentUser = null) where T : SongDto
    {
        var dto = _mapper.Map<T>(song);
        dto.CommentCount = await _commentRepository.CountCommentsAsync(comment => comment.ResourceId == song.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(song.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(song.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapChartAsync<T>(Chart chart, User? currentUser = null) where T : ChartDto
    {
        var dto = _mapper.Map<T>(chart);
        dto.CommentCount = await _commentRepository.CountCommentsAsync(comment => comment.ResourceId == chart.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(chart.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(chart.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapRecordAsync<T>(Record record, User? currentUser = null) where T : RecordDto
    {
        var dto = _mapper.Map<T>(record);
        dto.Position =
            await _recordRepository.CountRecordsAsync(r => r.ChartId == record.ChartId && r.Rks > record.Rks) + 1;
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(record.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(record.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapCommentAsync<T>(Comment comment, User? currentUser = null) where T : CommentDto
    {
        var dto = _mapper.Map<T>(comment);
        dto.ReplyCount = await _replyRepository.CountRepliesAsync(reply => reply.CommentId == comment.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(comment.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(comment.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapReplyAsync<T>(Reply reply, User? currentUser = null) where T : ReplyDto
    {
        var dto = _mapper.Map<T>(reply);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(reply.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(reply.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapApplicationAsync<T>(Application application, User? currentUser = null)
        where T : ApplicationDto
    {
        var dto = _mapper.Map<T>(application);
        dto.CommentCount = await _commentRepository.CountCommentsAsync(comment => comment.ResourceId == application.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(application.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(application.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapAnnouncementAsync<T>(Announcement announcement, User? currentUser = null)
        where T : AnnouncementDto
    {
        var dto = _mapper.Map<T>(announcement);
        dto.CommentCount =
            await _commentRepository.CountCommentsAsync(comment => comment.ResourceId == announcement.Id);
        dto.DateLiked = currentUser != null && await _likeRepository.LikeExistsAsync(announcement.Id, currentUser.Id)
            ? (await _likeRepository.GetLikeAsync(announcement.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }
}