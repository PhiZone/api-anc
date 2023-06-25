using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

/// <inheritdoc />
public class UserRepository : IUserRepository
{
    private readonly DataContext _context;

    public UserRepository(DataContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get a user by id.
    /// </summary>
    /// <param name="id">the <see cref="User.Id"/> of User</param>
    /// <returns>A User.</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public User GetUser(int id)
    {
        return _context.Users.FirstOrDefault(p => p.Id.Equals(id)) ??
               throw new KeyNotFoundException($"cannot found user id: {id}");
    }

    /// <summary>
    /// Get a user by username.
    /// </summary>
    /// <param name="name"><see cref="User.UserName"/></param>
    /// <returns>A User</returns>
    /// <exception cref="KeyNotFoundException"></exception>
    public User GetUser(string name)
    {
        return _context.Users.FirstOrDefault(p => p.UserName != null && p.UserName.Equals(name)) ??
               throw new KeyNotFoundException(
                   $"Cannot found username: {name}");
    }

    /// <summary>
    /// Check if the user exists.
    /// </summary>
    /// <param name="id">the <see cref="User.Id"/> of User</param>
    /// <returns>bool</returns>
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