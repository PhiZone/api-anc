using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChartSubmissionRepository : IChartSubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public ChartSubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<ChartSubmission>> GetChartSubmissionsAsync(string order, bool desc, int position,
        int take, string? search = null, Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartSubmissions.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart =>
                (chart.Song != null
                    ? chart.Song.Title.ToUpper().Contains(search) ||
                      (chart.Song.Edition != null && chart.Song.Edition.ToUpper().Contains(search)) ||
                      chart.Song.AuthorName.ToUpper().Contains(search) ||
                      (chart.Song.Description != null && chart.Song.Description.ToUpper().Contains(search))
                    : chart.SongSubmission!.Title.ToUpper().Contains(search) ||
                      (chart.SongSubmission.Edition != null &&
                       chart.SongSubmission.Edition.ToUpper().Contains(search)) ||
                      chart.SongSubmission.AuthorName.ToUpper().Contains(search) ||
                      (chart.SongSubmission.Description != null &&
                       chart.SongSubmission.Description.ToUpper().Contains(search))) ||
                (chart.Title != null && chart.Title.ToUpper().Contains(search)) ||
                chart.AuthorName.ToUpper().Contains(search) || (chart.Description != null &&
                                                                chart.Description.ToUpper().Contains(search)));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<ChartSubmission>> GetUserChartSubmissionsAsync(int userId, string order, bool desc,
        int position, int take, string? search = null, Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartSubmissions.Where(chart => chart.OwnerId == userId).OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart => (chart.Song != null
                                               ? chart.Song.Title.ToUpper().Contains(search) ||
                                                 (chart.Song.Edition != null &&
                                                  chart.Song.Edition.ToUpper().Contains(search)) ||
                                                 chart.Song.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.Song.Description != null &&
                                                  chart.Song.Description.ToUpper().Contains(search))
                                               : chart.SongSubmission!.Title.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Edition != null &&
                                                  chart.SongSubmission.Edition.ToUpper().Contains(search)) ||
                                                 chart.SongSubmission.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Description != null &&
                                                  chart.SongSubmission.Description.ToUpper().Contains(search))) ||
                                           (chart.Title != null && chart.Title.ToUpper().Contains(search)) ||
                                           chart.AuthorName.ToUpper().Contains(search) || (chart.Description != null &&
                                               chart.Description.ToUpper().Contains(search)));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ChartSubmission> GetChartSubmissionAsync(Guid id)
    {
        return (await _context.ChartSubmissions.FirstOrDefaultAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> ChartSubmissionExistsAsync(Guid id)
    {
        return (await _context.ChartSubmissions.AnyAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> CreateChartSubmissionAsync(ChartSubmission chart)
    {
        await _context.ChartSubmissions.AddAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartSubmissionAsync(ChartSubmission chart)
    {
        _context.ChartSubmissions.Update(chart);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartSubmissionAsync(Guid id)
    {
        _context.ChartSubmissions.Remove(
            (await _context.ChartSubmissions.FirstOrDefaultAsync(chart => chart.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartSubmissionsAsync(string? search = null,
        Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartSubmissions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart => (chart.Song != null
                                               ? chart.Song.Title.ToUpper().Contains(search) ||
                                                 (chart.Song.Edition != null &&
                                                  chart.Song.Edition.ToUpper().Contains(search)) ||
                                                 chart.Song.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.Song.Description != null &&
                                                  chart.Song.Description.ToUpper().Contains(search))
                                               : chart.SongSubmission!.Title.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Edition != null &&
                                                  chart.SongSubmission.Edition.ToUpper().Contains(search)) ||
                                                 chart.SongSubmission.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Description != null &&
                                                  chart.SongSubmission.Description.ToUpper().Contains(search))) ||
                                           (chart.Title != null && chart.Title.ToUpper().Contains(search)) ||
                                           chart.AuthorName.ToUpper().Contains(search) || (chart.Description != null &&
                                               chart.Description.ToUpper().Contains(search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountUserChartSubmissionsAsync(int userId, string? search = null,
        Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartSubmissions.Where(chart => chart.OwnerId == userId).AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(chart => (chart.Song != null
                                               ? chart.Song.Title.ToUpper().Contains(search) ||
                                                 (chart.Song.Edition != null &&
                                                  chart.Song.Edition.ToUpper().Contains(search)) ||
                                                 chart.Song.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.Song.Description != null &&
                                                  chart.Song.Description.ToUpper().Contains(search))
                                               : chart.SongSubmission!.Title.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Edition != null &&
                                                  chart.SongSubmission.Edition.ToUpper().Contains(search)) ||
                                                 chart.SongSubmission.AuthorName.ToUpper().Contains(search) ||
                                                 (chart.SongSubmission.Description != null &&
                                                  chart.SongSubmission.Description.ToUpper().Contains(search))) ||
                                           (chart.Title != null && chart.Title.ToUpper().Contains(search)) ||
                                           chart.AuthorName.ToUpper().Contains(search) || (chart.Description != null &&
                                               chart.Description.ToUpper().Contains(search)));
        }

        return await result.CountAsync();
    }
}