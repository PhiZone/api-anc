using Microsoft.EntityFrameworkCore;
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

    public async Task<ICollection<User>> GetUsersAsync(string order, bool desc, int position, int take)
    {
        return await _context.Users.OrderBy(order, desc).Skip(position).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<int> CountFollowersAsync(User user)
    {
        return await _context.UserRelations.Where(relation => relation.Followee == user).CountAsync();
    }

    public async Task<int> CountFolloweesAsync(User user)
    {
        return await _context.UserRelations.Where(relation => relation.Follower == user).CountAsync();
    }
}