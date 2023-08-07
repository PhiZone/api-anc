using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserService
{
    Task CreateUser(User user);
}