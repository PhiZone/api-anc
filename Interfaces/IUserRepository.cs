using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    Task<ICollection<User>> GetUsersAsync(string order, bool desc, int position, int take);

    Task<int> CountAsync();

    Task<int> CountFollowersAsync(User user);

    Task<int> CountFolloweesAsync(User user);
}