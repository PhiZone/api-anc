using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IUserRepository
{
    ICollection<User> GetUsers(string order, bool desc, int position, int take);
    User GetUser(int id);
    User GetUser(string name);
    bool UserExists(int id);
    bool UpdateUser(User user);
    bool Save();
}