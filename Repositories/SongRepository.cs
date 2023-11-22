using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class SongRepository(ApplicationDbContext context) : ISongRepository
{
    public async Task<ICollection<Song>> GetSongsAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var result = context.Songs.OrderBy(order, desc);

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

    public async Task<Song> GetSongAsync(Guid id)
    {
        return (await context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
    }

    public async Task<Song?> GetRandomSongAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var result = context.Songs.OrderBy(song => EF.Functions.Random()).AsQueryable();

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

        return await result.FirstOrDefaultAsync();
    }

    public async Task<ICollection<Chart>> GetSongChartsAsync(Guid id, List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<Chart, bool>>? predicate = null)
    {
        var song = (await context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
        var result = context.Charts.Where(chart => chart.Song.Id == song.Id).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chart =>
                (chart.Title != null && EF.Functions.Like(chart.Title.ToUpper(), search)) ||
                EF.Functions.Like(chart.AuthorName.ToUpper(), search) ||
                (chart.Description != null && EF.Functions.Like(chart.Description.ToUpper(), search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> SongExistsAsync(Guid id)
    {
        return (await context.Songs.AnyAsync(song => song.Id == id))!;
    }

    public async Task<bool> CreateSongAsync(Song song)
    {
        await context.Songs.AddAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongAsync(Song song)
    {
        context.Songs.Update(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongsAsync(IEnumerable<Song> songs)
    {
        context.Songs.UpdateRange(songs);
        return await SaveAsync();
    }

    public async Task<bool> RemoveSongAsync(Guid id)
    {
        context.Songs.Remove((await context.Songs.FirstOrDefaultAsync(song => song.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountSongsAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var result = context.Songs.AsQueryable();

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

    public async Task<int> CountSongChartsAsync(Guid id, string? search = null,
        Expression<Func<Chart, bool>>? predicate = null)
    {
        var song = (await context.Songs.FirstOrDefaultAsync(song => song.Id == id))!;
        var result = context.Charts.Where(chart => chart.Song.Id == song.Id);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chart =>
                (chart.Title != null && EF.Functions.Like(chart.Title.ToUpper(), search)) ||
                EF.Functions.Like(chart.AuthorName.ToUpper(), search) ||
                (chart.Description != null && EF.Functions.Like(chart.Description.ToUpper(), search)));
        }

        return await result.CountAsync();
    }
}