using AutoMapper;
using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class DtoMapper(IUserRelationRepository userRelationRepository, IRegionRepository regionRepository,
    ILikeRepository likeRepository, UserManager<User> userManager, IMapper mapper, ICommentRepository commentRepository,
    IReplyRepository replyRepository, IRecordRepository recordRepository, IChapterRepository chapterRepository,
    ISongRepository songRepository, IChartRepository chartRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IChartSubmissionRepository chartSubmissionRepository, IVolunteerVoteRepository volunteerVoteRepository,
    IPetQuestionRepository petQuestionRepository) : IDtoMapper
{
    public async Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto
    {
        var dto = mapper.Map<T>(user);
        dto.Role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
        dto.FollowerCount = await userRelationRepository.CountFollowersAsync(user.Id);
        dto.FolloweeCount = await userRelationRepository.CountFolloweesAsync(user.Id);
        dto.Region = (await regionRepository.GetRegionAsync(user.RegionId)).Code;
        // ReSharper disable once InvertIf
        if (currentUser != null && await userRelationRepository.RelationExistsAsync(currentUser.Id, user.Id))
        {
            var relation = await userRelationRepository.GetRelationAsync(currentUser.Id, user.Id);
            if (relation.Type != UserRelationType.Blacklisted) dto.DateFollowed = relation.DateCreated;
        }

        return dto;
    }

    public async Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await userManager.FindByIdAsync(userRelation.FollowerId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto
    {
        var user = await userManager.FindByIdAsync(userRelation.FolloweeId.ToString());
        return await MapUserAsync<T>(user!, currentUser);
    }

    public async Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null)
        where T : UserRelationDto
    {
        var dto = mapper.Map<T>(userRelation);
        dto.Follower = await MapFollowerAsync<UserDto>(userRelation, currentUser);
        dto.Followee = await MapFolloweeAsync<UserDto>(userRelation, currentUser);
        return dto;
    }

    public async Task<AdmissionDto<TAdmitter, TAdmittee>> MapChapterAdmissionAsync<TAdmitter, TAdmittee>(
        Admission admission, User? currentUser = null) where TAdmitter : ChapterDto where TAdmittee : SongDto
    {
        var dto = new AdmissionDto<TAdmitter, TAdmittee>
        {
            Admitter =
                await MapChapterAsync<TAdmitter>(await chapterRepository.GetChapterAsync(admission.AdmitterId),
                    currentUser),
            Admittee =
                await MapSongAsync<TAdmittee>(await songRepository.GetSongAsync(admission.AdmitteeId), currentUser),
            Status = admission.Status,
            Label = admission.Label,
            RequesterId = admission.RequesterId,
            RequesteeId = admission.RequesteeId,
            DateCreated = admission.DateCreated
        };
        return dto;
    }

    public async Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongAdmissionAsync<TAdmitter, TAdmittee>(
        Admission admission, User? currentUser = null) where TAdmitter : SongDto where TAdmittee : ChartSubmissionDto
    {
        var dto = new AdmissionDto<TAdmitter, TAdmittee>
        {
            Admitter =
                await MapSongAsync<TAdmitter>(await songRepository.GetSongAsync(admission.AdmitterId), currentUser),
            Admittee =
                await MapChartSubmissionAsync<TAdmittee>(
                    await chartSubmissionRepository.GetChartSubmissionAsync(admission.AdmitteeId), currentUser),
            Status = admission.Status,
            Label = admission.Label,
            RequesterId = admission.RequesterId,
            RequesteeId = admission.RequesteeId,
            DateCreated = admission.DateCreated
        };
        return dto;
    }

    public async Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongSubmissionAdmissionAsync<TAdmitter, TAdmittee>(
        Admission admission, User? currentUser = null) where TAdmitter : SongSubmissionDto
        where TAdmittee : ChartSubmissionDto
    {
        var dto = new AdmissionDto<TAdmitter, TAdmittee>
        {
            Admitter = mapper.Map<TAdmitter>(
                await songSubmissionRepository.GetSongSubmissionAsync(admission.AdmitterId)),
            Admittee =
                await MapChartSubmissionAsync<TAdmittee>(
                    await chartSubmissionRepository.GetChartSubmissionAsync(admission.AdmitteeId), currentUser),
            Status = admission.Status,
            Label = admission.Label,
            RequesterId = admission.RequesterId,
            RequesteeId = admission.RequesteeId,
            DateCreated = admission.DateCreated
        };
        return dto;
    }

    public async Task<T> MapChapterAsync<T>(Chapter chapter, User? currentUser = null) where T : ChapterDto
    {
        var dto = mapper.Map<T>(chapter);
        dto.CommentCount = await commentRepository.CountCommentsAsync(comment => comment.ResourceId == chapter.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(chapter.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(chapter.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapSongAsync<T>(Song song, User? currentUser = null) where T : SongDto
    {
        var dto = mapper.Map<T>(song);

        foreach (var levelType in Enum.GetValues<ChartLevel>())
            dto.ChartLevels.Add(new ChartLevelDto
            {
                LevelType = levelType,
                Count = await chartRepository.CountChartsAsync(predicate: chart =>
                    chart.SongId == song.Id && chart.LevelType == levelType)
            });

        dto.CommentCount = await commentRepository.CountCommentsAsync(comment => comment.ResourceId == song.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(song.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(song.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapSongChapterAsync<T>(Admission admission, User? currentUser = null)
        where T : ChapterAdmitterDto
    {
        var dto = await MapChapterAsync<T>(await chapterRepository.GetChapterAsync(admission.AdmitterId), currentUser);
        dto.Label = admission.Label;
        return dto;
    }

    public async Task<T> MapChapterSongAsync<T>(Admission admission, User? currentUser = null) where T : SongAdmitteeDto
    {
        var dto = await MapSongAsync<T>(await songRepository.GetSongAsync(admission.AdmitteeId), currentUser);
        dto.Label = admission.Label;
        return dto;
    }

    public async Task<T> MapChartAsync<T>(Chart chart, User? currentUser = null) where T : ChartDto
    {
        var dto = mapper.Map<T>(chart);
        dto.CommentCount = await commentRepository.CountCommentsAsync(comment => comment.ResourceId == chart.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(chart.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(chart.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapChartSubmissionAsync<T>(ChartSubmission chart, User? currentUser = null)
        where T : ChartSubmissionDto
    {
        var dto = mapper.Map<T>(chart);
        dto.DateVoted =
            currentUser != null && await volunteerVoteRepository.VolunteerVoteExistsAsync(chart.Id, currentUser.Id)
                ? (await volunteerVoteRepository.GetVolunteerVoteAsync(chart.Id, currentUser.Id)).DateCreated
                : null;
        return dto;
    }

    public async Task<T> MapRecordAsync<T>(Record record, User? currentUser = null) where T : RecordDto
    {
        var dto = mapper.Map<T>(record);
        dto.Position =
            await recordRepository.CountRecordsAsync(r => r.ChartId == record.ChartId && r.Rks > record.Rks) + 1;
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(record.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(record.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapCommentAsync<T>(Comment comment, User? currentUser = null) where T : CommentDto
    {
        var dto = mapper.Map<T>(comment);
        dto.ReplyCount = await replyRepository.CountRepliesAsync(reply => reply.CommentId == comment.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(comment.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(comment.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapReplyAsync<T>(Reply reply, User? currentUser = null) where T : ReplyDto
    {
        var dto = mapper.Map<T>(reply);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(reply.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(reply.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapApplicationAsync<T>(Application application, User? currentUser = null)
        where T : ApplicationDto
    {
        var dto = mapper.Map<T>(application);
        dto.CommentCount = await commentRepository.CountCommentsAsync(comment => comment.ResourceId == application.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(application.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(application.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapAnnouncementAsync<T>(Announcement announcement, User? currentUser = null)
        where T : AnnouncementDto
    {
        var dto = mapper.Map<T>(announcement);
        dto.CommentCount = await commentRepository.CountCommentsAsync(comment => comment.ResourceId == announcement.Id);
        dto.DateLiked = currentUser != null && await likeRepository.LikeExistsAsync(announcement.Id, currentUser.Id)
            ? (await likeRepository.GetLikeAsync(announcement.Id, currentUser.Id)).DateCreated
            : null;
        return dto;
    }

    public async Task<T> MapNotificationAsync<T>(Notification notification, User? currentUser = null)
        where T : NotificationDto
    {
        var dto = mapper.Map<T>(notification);
        if (notification.OperatorId != null)
            dto.Operator =
                await MapUserAsync<UserDto>(
                    (await userManager.FindByIdAsync(notification.OperatorId.Value.ToString()))!, currentUser);

        return dto;
    }

    public async Task<T> MapPetAnswerAsync<T>(PetAnswer answer) where T : PetAnswerDto
    {
        var dto = mapper.Map<T>(answer);
        dto.Question1 = MapPetQuestion(await petQuestionRepository.GetPetQuestionAsync(answer.Question1));
        dto.Question2 = MapPetQuestion(await petQuestionRepository.GetPetQuestionAsync(answer.Question2));
        dto.Question3 = MapPetQuestion(await petQuestionRepository.GetPetQuestionAsync(answer.Question3));
        return dto;
    }

    private static PetQuestionDto MapPetQuestion(PetQuestion question)
    {
        return new PetQuestionDto
        {
            Position = question.Position,
            Type = question.Type,
            Content = question.Content,
            Language = question.Language
        };
    }
}