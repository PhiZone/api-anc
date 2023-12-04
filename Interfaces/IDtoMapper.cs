using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IDtoMapper
{
    Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto;

    Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;

    Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;

    Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserRelationDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapChapterAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : ChapterDto where TAdmittee : SongDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapCollectionAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : CollectionDto where TAdmittee : ChartDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : SongDto where TAdmittee : ChartSubmissionDto;

    Task<AdmissionDto<TAdmitter, TAdmittee>> MapSongSubmissionAdmissionAsync<TAdmitter, TAdmittee>(Admission admission,
        User? currentUser = null) where TAdmitter : SongSubmissionDto where TAdmittee : ChartSubmissionDto;

    Task<T> MapChapterAsync<T>(Chapter chapter, User? currentUser = null) where T : ChapterDto;

    Task<T> MapCollectionAsync<T>(Collection collection, User? currentUser = null) where T : CollectionDto;

    Task<T> MapSongAsync<T>(Song song, User? currentUser = null) where T : SongDto;

    Task<T> MapSongChapterAsync<T>(Admission admission, User? currentUser = null) where T : ChapterAdmitterDto;

    Task<T> MapChapterSongAsync<T>(Admission admission, User? currentUser = null) where T : SongAdmitteeDto;

    Task<T> MapChartAsync<T>(Chart chart, User? currentUser = null) where T : ChartDto;

    Task<T> MapChartCollectionAsync<T>(Admission admission, User? currentUser = null) where T : CollectionAdmitterDto;

    Task<T> MapCollectionChartAsync<T>(Admission admission, User? currentUser = null) where T : ChartAdmitteeDto;

    Task<T> MapChartSubmissionAsync<T>(ChartSubmission chart, User? currentUser = null) where T : ChartSubmissionDto;

    Task<T> MapRecordAsync<T>(Record record, User? currentUser = null) where T : RecordDto;

    Task<T> MapCommentAsync<T>(Comment comment, User? currentUser = null) where T : CommentDto;

    Task<T> MapReplyAsync<T>(Reply reply, User? currentUser = null) where T : ReplyDto;

    Task<T> MapApplicationAsync<T>(Application application, User? currentUser = null) where T : ApplicationDto;

    Task<T> MapAnnouncementAsync<T>(Announcement announcement, User? currentUser = null) where T : AnnouncementDto;

    Task<T> MapNotificationAsync<T>(Notification notification, User? currentUser = null) where T : NotificationDto;

    Task<T> MapPetAnswerAsync<T>(PetAnswer answer) where T : PetAnswerDto;
}