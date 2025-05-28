using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class LikeRepository(ApplicationDbContext context) : ILikeRepository
{
    public async Task<ICollection<Like>> GetLikesAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<Like, bool>>? predicate = null)
    {
        var result = context.Likes.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Like> GetLikeAsync(Guid id)
    {
        return (await context.Likes.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<Like> GetLikeAsync(Guid resourceId, int userId)
    {
        return (await context.Likes.FirstOrDefaultAsync(like =>
            like.Resource.Id == resourceId && like.OwnerId == userId))!;
    }

    public async Task<bool> LikeExistsAsync(Guid id)
    {
        return (await context.Likes.AnyAsync(like => like.Id == id))!;
    }

    public async Task<bool> LikeExistsAsync(Guid resourceId, int userId)
    {
        return (await context.Likes.AnyAsync(like => like.Resource.Id == resourceId && like.OwnerId == userId))!;
    }

    public async Task<bool> CreateLikeAsync(Like like)
    {
        await context.Likes.AddAsync(like);
        return await SaveAsync();
    }

    public async Task<bool> RemoveLikeAsync(Guid id)
    {
        context.Likes.Remove((await context.Likes.FirstOrDefaultAsync(like => like.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountLikesAsync(Expression<Func<Like, bool>>? predicate = null)
    {
        var result = context.Likes.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}