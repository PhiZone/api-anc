using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class SongSubmissionRepository : ISongSubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public SongSubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<SongSubmission>> GetSongSubmissionsAsync(List<string> order, List<bool> desc,
        int position, int take, string? search = null, Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = _context.SongSubmissions.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(song => EF.Functions.Like(song.Title.ToUpper(), search) ||
                                          (song.Edition != null && EF.Functions.Like(song.Edition.ToUpper(), search)) ||
                                          EF.Functions.Like(song.AuthorName.ToUpper(), search) ||
                                          (song.Description != null &&
                                           EF.Functions.Like(song.Description.ToUpper(), search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<SongSubmission>> GetUserSongSubmissionsAsync(int userId, List<string> order,
        List<bool> desc, int position, int take, string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = _context.SongSubmissions.Where(song => song.OwnerId == userId).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(song => EF.Functions.Like(song.Title.ToUpper(), search) ||
                                          (song.Edition != null && EF.Functions.Like(song.Edition.ToUpper(), search)) ||
                                          EF.Functions.Like(song.AuthorName.ToUpper(), search) ||
                                          (song.Description != null &&
                                           EF.Functions.Like(song.Description.ToUpper(), search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<SongSubmission> GetSongSubmissionAsync(Guid id)
    {
        return (await _context.SongSubmissions.FirstOrDefaultAsync(song => song.Id == id))!;
    }

    public async Task<bool> SongSubmissionExistsAsync(Guid id)
    {
        return (await _context.SongSubmissions.AnyAsync(song => song.Id == id))!;
    }

    public async Task<bool> CreateSongSubmissionAsync(SongSubmission song)
    {
        await _context.SongSubmissions.AddAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongSubmissionAsync(SongSubmission song)
    {
        _context.SongSubmissions.Update(song);
        return await SaveAsync();
    }

    public async Task<bool> RemoveSongSubmissionAsync(Guid id)
    {
        _context.SongSubmissions.Remove((await _context.SongSubmissions.FirstOrDefaultAsync(song => song.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountSongSubmissionsAsync(string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = _context.SongSubmissions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(song => EF.Functions.Like(song.Title.ToUpper(), search) ||
                                          (song.Edition != null && EF.Functions.Like(song.Edition.ToUpper(), search)) ||
                                          EF.Functions.Like(song.AuthorName.ToUpper(), search) ||
                                          (song.Description != null &&
                                           EF.Functions.Like(song.Description.ToUpper(), search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountUserSongSubmissionsAsync(int userId, string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null)
    {
        var result = _context.SongSubmissions.Where(song => song.OwnerId == userId).AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(song => EF.Functions.Like(song.Title.ToUpper(), search) ||
                                          (song.Edition != null && EF.Functions.Like(song.Edition.ToUpper(), search)) ||
                                          EF.Functions.Like(song.AuthorName.ToUpper(), search) ||
                                          (song.Description != null &&
                                           EF.Functions.Like(song.Description.ToUpper(), search)));
        }

        return await result.CountAsync();
    }
}