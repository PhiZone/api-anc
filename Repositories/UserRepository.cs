using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<User>> GetUsersAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var result = _context.Users.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<int> CountAsync(string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var result = _context.Users.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(search)) ||
                (user.Tag != null && user.Tag.ToUpper().Contains(search)) ||
                (user.Biography != null && user.Biography.ToUpper().Contains(search)) ||
                user.Language.ToUpper().Contains(search));
        }

        return await result.CountAsync();
    }
}