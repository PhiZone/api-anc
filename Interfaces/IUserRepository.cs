using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    ICollection<User> GetUsers(string order, bool desc, int position, int take);
}