using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class SongRepository : ISongRepository
{
    private readonly ApplicationDbContext _context;

    public SongRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Song>> GetSongsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var result = _context.Songs.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(song => song.Title.ToUpper().Like(search) ||
                                          (song.Edition != null && song.Edition.ToUpper().Like(search)) ||
                                          song.AuthorName.ToUpper().Like(search) || (song.Description != null &&
                                              song.Description.ToUpper().Like(search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Song> GetSongAsync(Guid id)
    {
        return (await _context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
    }

    public async Task<ICollection<Chart>> GetSongChartsAsync(Guid id, string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Chart, bool>>? predicate = null)
    {
        var song = (await _context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
        var result = _context.Charts.Where(chart => chart.Song.Id == song.Id).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart =>
                (chart.Title != null && chart.Title.ToUpper().Like(search)) ||
                chart.AuthorName.ToUpper().Like(search) ||
                (chart.Description != null && chart.Description.ToUpper().Like(search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> SongExistsAsync(Guid id)
    {
        return (await _context.Songs.AnyAsync(song => song.Id == id))!;
    }

    public async Task<bool> CreateSongAsync(Song song)
    {
        await _context.Songs.AddAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongAsync(Song song)
    {
        _context.Songs.Update(song);
        return await SaveAsync();
    }

    public async Task<bool> RemoveSongAsync(Guid id)
    {
        _context.Songs.Remove((await _context.Songs.FirstOrDefaultAsync(song => song.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountSongsAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var result = _context.Songs.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(song => song.Title.ToUpper().Like(search) ||
                                          (song.Edition != null && song.Edition.ToUpper().Like(search)) ||
                                          song.AuthorName.ToUpper().Like(search) || (song.Description != null &&
                                              song.Description.ToUpper().Like(search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountSongChartsAsync(Guid id, string? search = null,
        Expression<Func<Chart, bool>>? predicate = null)
    {
        var song = (await _context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
        var result = _context.Charts.Where(chart => chart.Song.Id == song.Id);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart =>
                (chart.Title != null && chart.Title.ToUpper().Like(search)) ||
                chart.AuthorName.ToUpper().Like(search) ||
                (chart.Description != null && chart.Description.ToUpper().Like(search)));
        }

        return await result.CountAsync();
    }
}