using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChartRepository : IChartRepository
{
    private readonly ApplicationDbContext _context;

    public ChartRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Chart>> GetChartsAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Chart, bool>>? predicate = null)
    {
        var result = _context.Charts.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chart => EF.Functions.Like(chart.Song.Title.ToUpper(), search) ||
                                           (chart.Song.Edition != null &&
                                            EF.Functions.Like(chart.Song.Edition.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.Song.AuthorName.ToUpper(), search) ||
                                           (chart.Song.Description != null &&
                                            EF.Functions.Like(chart.Song.Description.ToUpper(), search)) ||
                                           (chart.Title != null && EF.Functions.Like(chart.Title.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.AuthorName.ToUpper(), search) ||
                                           (chart.Description != null &&
                                            EF.Functions.Like(chart.Description.ToUpper(), search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Chart> GetChartAsync(Guid id)
    {
        return (await _context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!;
    }

    public async Task<Chart?> GetRandomChartAsync(string? search = null, Expression<Func<Chart, bool>>? predicate = null)
    {
        var result = _context.Charts.OrderBy(chart => EF.Functions.Random()).AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chart => EF.Functions.Like(chart.Song.Title.ToUpper(), search) ||
                                           (chart.Song.Edition != null &&
                                            EF.Functions.Like(chart.Song.Edition.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.Song.AuthorName.ToUpper(), search) ||
                                           (chart.Song.Description != null &&
                                            EF.Functions.Like(chart.Song.Description.ToUpper(), search)) ||
                                           (chart.Title != null && EF.Functions.Like(chart.Title.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.AuthorName.ToUpper(), search) ||
                                           (chart.Description != null &&
                                            EF.Functions.Like(chart.Description.ToUpper(), search)));
        }
        
        return await result.FirstOrDefaultAsync();
    }

    public async Task<ICollection<Record>> GetChartRecordsAsync(Guid id, string order, bool desc, int position,
        int take, Expression<Func<Record, bool>>? predicate = null)
    {
        var chart = (await _context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!;
        var result = _context.Records.Where(record => record.Chart.Id == chart.Id).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> ChartExistsAsync(Guid id)
    {
        return (await _context.Charts.AnyAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> CreateChartAsync(Chart chart)
    {
        await _context.Charts.AddAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAsync(Chart chart)
    {
        _context.Charts.Update(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartsAsync(IEnumerable<Chart> charts)
    {
        _context.Charts.UpdateRange(charts);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAsync(Guid id)
    {
        _context.Charts.Remove((await _context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartsAsync(string? search = null, Expression<Func<Chart, bool>>? predicate = null)
    {
        var result = _context.Charts.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chart => EF.Functions.Like(chart.Song.Title.ToUpper(), search) ||
                                           (chart.Song.Edition != null &&
                                            EF.Functions.Like(chart.Song.Edition.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.Song.AuthorName.ToUpper(), search) ||
                                           (chart.Song.Description != null &&
                                            EF.Functions.Like(chart.Song.Description.ToUpper(), search)) ||
                                           (chart.Title != null && EF.Functions.Like(chart.Title.ToUpper(), search)) ||
                                           EF.Functions.Like(chart.AuthorName.ToUpper(), search) ||
                                           (chart.Description != null &&
                                            EF.Functions.Like(chart.Description.ToUpper(), search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountChartRecordsAsync(Guid id,
        Expression<Func<Record, bool>>? predicate = null)
    {
        var chart = (await _context.Charts.FirstOrDefaultAsync(chart => chart.Id == id))!;
        var result = _context.Records.Where(record => record.Chart.Id == chart.Id);

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}