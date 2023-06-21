using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
        _context = context;
    }

    public ICollection<User> GetUsers()
    {
        return _context.Users.OrderBy(p => p.Id).ToList();
    }

    public User GetUser(int id)
    {
        return _context.Users.FirstOrDefault(p => p.Id.Equals(id)) ?? throw new KeyNotFoundException();
    }

    public User GetUser(string name)
    {
        return _context.Users.FirstOrDefault(p => p.UserName != null && p.UserName.Equals(name)) ??
               throw new KeyNotFoundException();
    }

    public bool UserExists(int id)
    {
        return _context.Users.Any(p => p.Id.Equals(id));
    }

    public bool Save()
    {
        var saved = _context.SaveChanges();
        return saved > 0;
    }

    public bool UpdateUser(User user)
    {
        _context.Update(user);
        return Save();
    }
}