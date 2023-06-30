using PhiZoneApi.Dtos;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IDtoMapper
{
    Task<T> MapUserAsync<T>(User user) where T : UserDto;
}