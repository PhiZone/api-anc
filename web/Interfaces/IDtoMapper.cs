using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IDtoMapper
{
    T MapUser<T>(User user) where T : UserDto;

    T MapFollowee<T>(User user, int? currentUserId = null) where T : UserDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapChapterAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : ChapterDto where TAdmittee : SongDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapCollectionAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : CollectionDto where TAdmittee : ChartDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : SongDto where TAdmittee : ChartSubmissionDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongSubmissionAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : SongSubmissionDto where TAdmittee : ChartSubmissionDto;

    T MapChapter<T>(Chapter chapter) where T : ChapterDto;

    T MapCollection<T>(Collection collection) where T : CollectionDto;

    T MapSong<T>(Song song, bool anonymize = false) where T : SongDto;

    Task<T> MapSongChapterAsync<T>(Admission admission, User? currentUser = null) where T : ChapterAdmitterDto;

    Task<T> MapChapterSongAsync<T>(Admission admission, User? currentUser) where T : SongAdmitteeDto;

    T MapChart<T>(Chart chart, bool anonymize = false) where T : ChartDto;

    T MapChartAsset<T>(ChartAsset asset, bool anonymize = false) where T : ChartAssetDto;

    Task<T> MapChartCollectionAsync<T>(Admission admission, User? currentUser = null) where T : CollectionAdmitterDto;

    Task<T> MapCollectionChartAsync<T>(Admission admission, User? currentUser = null) where T : ChartAdmitteeDto;

    T MapSongSubmission<T>(SongSubmission song, User? currentUser = null) where T : SongSubmissionDto;

    T MapChartSubmission<T>(ChartSubmission chart) where T : ChartSubmissionDto;

    T MapRecord<T>(Record record, bool anonymize = false) where T : RecordDto;

    T MapComment<T>(Comment comment) where T : CommentDto;

    T MapReply<T>(Reply reply) where T : ReplyDto;

    T MapApplication<T>(Application application) where T : ApplicationDto;

    T MapAnnouncement<T>(Announcement announcement) where T : AnnouncementDto;

    T MapEvent<T>(Event eventEntity) where T : EventDto;

    Task<T> MapEventDivisionAsync<T>(EventDivision eventDivision) where T : EventDivisionDto;

    T MapEventTeam<T>(EventTeam eventTeam) where T : EventTeamDto;

    T MapNotification<T>(Notification notification) where T : NotificationDto;

    Task<T> MapPetAnswerAsync<T>(PetAnswer answer) where T : PetAnswerDto;

    T MapGhostToUser<T>(TapGhost ghost) where T : UserDto;

    T MapGhostToUserDetailed<T>(TapGhost ghost) where T : UserDetailedDto;
}