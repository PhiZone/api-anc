using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserService
{
    Task CreateUser(User user);

    Task<bool> IsBlacklisted(int user1, int user2);
}