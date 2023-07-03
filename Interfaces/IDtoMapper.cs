using PhiZoneApi.Dtos.Responses;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IDtoMapper
{
    Task<T> MapUserAsync<T>(User user, User? currentUser = null) where T : UserDto;

    Task<T> MapFollowerAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;

    Task<T> MapFolloweeAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserDto;
    Task<T> MapUserRelationAsync<T>(UserRelation userRelation, User? currentUser = null) where T : UserRelationDto;
}