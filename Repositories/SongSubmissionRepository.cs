using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class SongSubmissionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : ISongSubmissionRepository
{
    public async Task<ICollection<SongSubmission>> GetSongSubmissionsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = context.SongSubmissions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<SongSubmission>> GetUserSongSubmissionsAsync(int userId, List<string> order,
        List<bool> desc, int position, int take,
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = context.SongSubmissions.Where(song => song.OwnerId == userId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<SongSubmission> GetSongSubmissionAsync(Guid id)
    {
        return (await context.SongSubmissions.FirstOrDefaultAsync(song => song.Id == id))!;
    }

    public async Task<bool> SongSubmissionExistsAsync(Guid id)
    {
        return (await context.SongSubmissions.AnyAsync(song => song.Id == id))!;
    }

    public async Task<bool> CreateSongSubmissionAsync(SongSubmission song)
    {
        await context.SongSubmissions.AddAsync(song);
        await meilisearchService.AddAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongSubmissionAsync(SongSubmission song)
    {
        context.SongSubmissions.Update(song);
        await meilisearchService.UpdateAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> RemoveSongSubmissionAsync(Guid id)
    {
        context.SongSubmissions.Remove((await context.SongSubmissions.FirstOrDefaultAsync(song => song.Id == id))!);
        await meilisearchService.DeleteAsync<SongSubmission>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountSongSubmissionsAsync(
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = context.SongSubmissions.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountUserSongSubmissionsAsync(int userId,
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = context.SongSubmissions.Where(song => song.OwnerId == userId).AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}