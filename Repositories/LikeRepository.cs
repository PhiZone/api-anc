using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;
// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class LikeRepository : ILikeRepository
{
    private readonly ApplicationDbContext _context;

    public LikeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Like>> GetLikesAsync(string order, bool desc, int position, int take, Expression<Func<Like, bool>>? predicate = null)
    {
        var result = _context.Likes.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Like> GetLikeAsync(Guid id)
    {
        return (await _context.Likes.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<Like> GetLikeAsync(Guid resourceId, int userId)
    {
        return (await _context.Likes.FirstOrDefaultAsync(
            like => like.Resource.Id == resourceId && like.UserId == userId))!;
    }

    public async Task<bool> LikeExistsAsync(Guid id)
    {
        return (await _context.Likes.AnyAsync(like => like.Id == id))!;
    }

    public async Task<bool> LikeExistsAsync(Guid resourceId, int userId)
    {
        return (await _context.Likes.AnyAsync(
            like => like.Resource.Id == resourceId && like.UserId == userId))!;
    }

    public async Task<bool> CreateLikeAsync(Like like)
    {
        await _context.AddAsync(like);
        return await SaveAsync();
    }

    public async Task<bool> RemoveLikeAsync(Guid id)
    {
        _context.Likes.Remove((await _context.Likes.FirstOrDefaultAsync(like => like.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAsync(Expression<Func<Like, bool>>? predicate = null)
    {
        var result = _context.Likes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}