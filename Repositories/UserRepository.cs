using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public ICollection<User> GetUsers(string order, bool desc, int position, int take)
    {
        return _context.Users.OrderBy(order, desc).Skip(position).Take(take).ToList();
    }
}