using AutoMapper;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class DtoMapper(
    IMapper mapper,
    IChapterRepository chapterRepository,
    ICollectionRepository collectionRepository,
    ISongRepository songRepository,
    IChartRepository chartRepository,
    IEventTeamRepository eventTeamRepository,
    IEventResourceRepository eventResourceRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IChartSubmissionRepository chartSubmissionRepository,
    IPetQuestionRepository petQuestionRepository) : IDtoMapper
{
    public T MapUser<T>(User user) where T : UserDto
    {
        var dto = mapper.Map<T>(user);
        dto.Role = user.Role.ToString();
        dto.DateFollowed = user.FollowerRelations.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapFollowee<T>(User user, int? currentUserId = null) where T : UserDto
    {
        var dto = mapper.Map<T>(user);
        dto.Role = user.Role.ToString();
        dto.DateFollowed = user.FollowerRelations
            .FirstOrDefault(e => currentUserId != null && e.FollowerId == currentUserId)
            ?.DateCreated;
        return dto;
    }

    public async Task<AdmissionDto<TAdmitter, TAdmittee>> MapChapterAdmissionAsync<TAdmitter, TAdmittee>(
        Admission admission, User? currentUser = null) where TAdmitter : ChapterDto where TAdmittee : SongDto
    {
        var dto = new AdmissionDto<TAdmitter, TAdmittee>
        {
            Admitter =
                MapChapter<TAdmitter>(
                    await chapterRepository.GetChapterAsync(admission.AdmitterId, currentUser?.Id)),
            Admittee = MapSong<TAdmittee>(await songRepository.GetSongAsync(admission.AdmitteeId, currentUser?.Id)),
            Status = admission.Status,
            Label = admission.Label,
            RequesterId = admission.RequesterId,
            RequesteeId = admission.RequesteeId,
            DateCreated = admission.DateCreated
        };
        return dto;
    }

    public async Task<AdmissionDto<TAdmitter, TAdmittee>> MapCollectionAdmissionAsync<TAdmitter, TAdmittee>(
        Admission admission, User? currentUser = null) where TAdmitter : CollectionDto where TAdmittee : ChartDto
    {
        var dto = new AdmissionDto<TAdmitter, TAdmittee>
        {
            Admitter =
                MapCollection<TAdmitter>(
                    await collectionRepository.GetCollectionAsync(admission.AdmitterId, currentUser?.Id)),
            Admittee =
                MapChart<TAdmittee>(await chartRepository.GetChartAsync(admission.AdmitteeId, currentUser?.Id)),
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
            Admitter = MapSong<TAdmitter>(await songRepository.GetSongAsync(admission.AdmitterId, currentUser?.Id)),
            Admittee =
                MapChartSubmission<TAdmittee>(
                    await chartSubmissionRepository.GetChartSubmissionAsync(admission.AdmitteeId, currentUser?.Id)),
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
            Admitter =
                mapper.Map<TAdmitter>(await songSubmissionRepository.GetSongSubmissionAsync(admission.AdmitterId)),
            Admittee =
                MapChartSubmission<TAdmittee>(
                    await chartSubmissionRepository.GetChartSubmissionAsync(admission.AdmitteeId, currentUser?.Id)),
            Status = admission.Status,
            Label = admission.Label,
            RequesterId = admission.RequesterId,
            RequesteeId = admission.RequesteeId,
            DateCreated = admission.DateCreated
        };
        return dto;
    }

    public T MapChapter<T>(Chapter chapter) where T : ChapterDto
    {
        var dto = mapper.Map<T>(chapter);
        dto.DateLiked = chapter.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapCollection<T>(Collection collection) where T : CollectionDto
    {
        var dto = mapper.Map<T>(collection);
        dto.DateLiked = collection.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapSong<T>(Song song, bool anonymize = false) where T : SongDto
    {
        var dto = mapper.Map<T>(song);
        anonymize = anonymize || song.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value) ||
                    song.Charts.Any(e => e.EventPresences.Any(f =>
                        f.IsAnonymous != null && f.IsAnonymous.Value && f.Team != null &&
                        f.Team.Participations.Any(g => g.ParticipantId == song.OwnerId)));
        foreach (var levelType in Enum.GetValues<ChartLevel>())
            dto.ChartLevels.Add(new ChartLevelDto
            {
                LevelType = levelType, Count = song.Charts.Count(e => e.LevelType == levelType)
            });
        dto.DateLiked = song.Likes.FirstOrDefault()?.DateCreated;
        if (anonymize)
        {
            if (dto.IsOriginal) dto.AuthorName = null;
            dto.OwnerId = null;
        }

        return dto;
    }

    public async Task<T> MapSongChapterAsync<T>(Admission admission, User? currentUser = null)
        where T : ChapterAdmitterDto
    {
        var dto = MapChapter<T>(await chapterRepository.GetChapterAsync(admission.AdmitterId, currentUser?.Id));
        dto.Label = admission.Label;
        return dto;
    }

    public async Task<T> MapChapterSongAsync<T>(Admission admission, User? currentUser = null) where T : SongAdmitteeDto
    {
        var dto = MapSong<T>(await songRepository.GetSongAsync(admission.AdmitteeId, currentUser?.Id));
        dto.Label = admission.Label;
        return dto;
    }

    public T MapChart<T>(Chart chart, bool anonymize = false) where T : ChartDto
    {
        var dto = mapper.Map<T>(chart);
        anonymize = anonymize || chart.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value);
        dto.Song = MapSong<SongDto>(chart.Song, anonymize);
        dto.DateLiked = chart.Likes.FirstOrDefault()?.DateCreated;
        if (anonymize)
        {
            dto.AuthorName = null;
            dto.OwnerId = null;
        }

        return dto;
    }

    public async Task<T> MapChartCollectionAsync<T>(Admission admission, User? currentUser = null)
        where T : CollectionAdmitterDto
    {
        var dto = MapCollection<T>(await collectionRepository.GetCollectionAsync(admission.AdmitterId,
            currentUser?.Id));
        dto.Label = admission.Label;
        return dto;
    }

    public async Task<T> MapCollectionChartAsync<T>(Admission admission, User? currentUser = null)
        where T : ChartAdmitteeDto
    {
        var dto = MapChart<T>(await chartRepository.GetChartAsync(admission.AdmitteeId, currentUser?.Id));
        dto.Label = admission.Label;
        return dto;
    }

    public T MapChartSubmission<T>(ChartSubmission chart) where T : ChartSubmissionDto
    {
        var dto = mapper.Map<T>(chart);
        if (chart.Song != null) dto.Song = MapSong<SongDto>(chart.Song);
        if (chart.SongSubmission != null) dto.SongSubmission = mapper.Map<SongSubmissionDto>(chart.SongSubmission);
        dto.DateVoted = chart.VolunteerVotes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapRecord<T>(Record record, bool anonymize = false) where T : RecordDto
    {
        var dto = mapper.Map<T>(record);
        anonymize = anonymize || record.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value);
        dto.Owner = MapUser<UserDto>(record.Owner);
        dto.DateLiked = record.Likes.FirstOrDefault()?.DateCreated;
        if (anonymize)
        {
            dto.OwnerId = null;
            dto.Owner = null;
        }

        return dto;
    }

    public T MapComment<T>(Comment comment) where T : CommentDto
    {
        var dto = mapper.Map<T>(comment);
        dto.Owner = MapUser<UserDto>(comment.Owner);
        dto.DateLiked = comment.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapReply<T>(Reply reply) where T : ReplyDto
    {
        var dto = mapper.Map<T>(reply);
        dto.Owner = MapUser<UserDto>(reply.Owner);
        dto.DateLiked = reply.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapApplication<T>(Application application) where T : ApplicationDto
    {
        var dto = mapper.Map<T>(application);
        dto.DateLiked = application.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapAnnouncement<T>(Announcement announcement) where T : AnnouncementDto
    {
        var dto = mapper.Map<T>(announcement);
        dto.DateLiked = announcement.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public T MapEvent<T>(Event eventEntity) where T : EventDto
    {
        var dto = mapper.Map<T>(eventEntity);
        dto.Divisions = eventEntity.Divisions.Where(e => e.DateUnveiled <= DateTimeOffset.UtcNow)
            .Select(e => new DivisionDto { Title = e.Title, Subtitle = e.Subtitle, Type = e.Type, Status = e.Status })
            .ToList();
        dto.Hosts = eventEntity.Hostships.Where(e => e.IsUnveiled)
            .Select(e =>
            {
                var host = MapUser<PositionalUserDto>(e.User);
                host.Position = e.Position;
                return host;
            })
            .ToList();
        dto.DateLiked = eventEntity.Likes.FirstOrDefault()?.DateCreated;
        return dto;
    }

    public async Task<T> MapEventDivisionAsync<T>(EventDivision eventDivision) where T : EventDivisionDto
    {
        var dto = mapper.Map<T>(eventDivision);
        dto.TeamCount = await eventTeamRepository.CountEventTeamsAsync(e => e.Status != ParticipationStatus.Banned &&
                                                                            e.DivisionId == eventDivision.Id);
        dto.EntryCount =
            await eventResourceRepository.CountResourcesAsync(eventDivision.Id, e => e.Type == EventResourceType.Entry);
        dto.DateLiked = eventDivision.Likes.FirstOrDefault()?.DateCreated;
        var team = eventDivision.Teams.FirstOrDefault();
        dto.Team = team != null ? MapEventTeam<EventTeamDto>(team) : null;
        return dto;
    }

    public T MapEventTeam<T>(EventTeam eventTeam) where T : EventTeamDto
    {
        var dto = mapper.Map<T>(eventTeam);
        dto.DateLiked = eventTeam.Likes.FirstOrDefault()?.DateCreated;
        dto.Participants = eventTeam.Participations.Select(e =>
            {
                var userDto = MapUser<PositionalUserDto>(e.Participant);
                userDto.Position = e.Position;
                return userDto;
            })
            .ToList();
        return dto;
    }

    public T MapNotification<T>(Notification notification) where T : NotificationDto
    {
        var dto = mapper.Map<T>(notification);
        if (notification.Operator != null) dto.Operator = MapUser<UserDto>(notification.Operator);
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