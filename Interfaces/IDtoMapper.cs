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
}