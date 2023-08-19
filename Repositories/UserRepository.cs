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
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
                EF.Functions.Like(user.Language.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<User?> GetUserByTapUnionId(string unionId)
    {
        return await _context.Users.FirstOrDefaultAsync(user => user.TapUnionId == unionId);
    }

    public async Task<int> CountUsersAsync(string? search = null, Expression<Func<User, bool>>? predicate = null)
    {
        var result = _context.Users.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(user =>
                (user.NormalizedUserName != null && EF.Functions.Like(user.NormalizedUserName, search)) ||
                (user.Tag != null && EF.Functions.Like(user.Tag.ToUpper(), search)) ||
                (user.Biography != null && EF.Functions.Like(user.Biography.ToUpper(), search)) ||
                EF.Functions.Like(user.Language.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}