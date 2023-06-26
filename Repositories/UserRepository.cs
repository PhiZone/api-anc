using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
        _context = context;
    }

    public User GetUser(int id)
    {
        return _context.Users.FirstOrDefault(p => p.Id.Equals(id)) ??
               throw new KeyNotFoundException($"Cannot find user with id {id}");
    }

    public User GetUser(string name)
    {
        return _context.Users.FirstOrDefault(p => p.UserName != null && p.UserName.Equals(name)) ??
               throw new KeyNotFoundException(
                   $"Cannot find user with name {name}");
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

    public ICollection<User> GetUsers(string order, bool desc, int position, int take)
    {
        return _context.Users.OrderBy(order, desc).Skip(position).Take(take).ToList();
    }
}