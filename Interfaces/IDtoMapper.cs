using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IDtoMapper
{
    Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto;

    Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;

    Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;

    Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserRelationDto;

    Task<T> MapChapterAsync<T>(Chapter chapter, User? currentUser = null) where T : ChapterDto;

    Task<T> MapSongAsync<T>(Song song, User? currentUser = null) where T : SongDto;

    Task<T> MapChartAsync<T>(Chart chart, User? currentUser = null) where T : ChartDto;

    Task<T> MapRecordAsync<T>(Record record, User? currentUser = null) where T : RecordDto;

    Task<T> MapCommentAsync<T>(Comment comment, User? currentUser = null) where T : CommentDto;

    Task<T> MapReplyAsync<T>(Reply reply, User? currentUser = null) where T : ReplyDto;

    Task<T> MapApplicationAsync<T>(Application application, User? currentUser = null) where T : ApplicationDto;

    Task<T> MapAnnouncementAsync<T>(Announcement announcement, User? currentUser = null) where T : AnnouncementDto;
}