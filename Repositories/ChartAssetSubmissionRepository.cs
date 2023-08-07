using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ChartAssetSubmissionRepository : IChartAssetSubmissionRepository
{
    private readonly ApplicationDbContext _context;

    public ChartAssetSubmissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<ChartAssetSubmission>> GetChartAssetSubmissionsAsync(string order, bool desc,
        int position, int take,
        Expression<Func<ChartAssetSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartAssetSubmissions.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ChartAssetSubmission> GetChartAssetSubmissionAsync(Guid id)
    {
        return (await _context.ChartAssetSubmissions.FirstOrDefaultAsync(chartAssetSubmission =>
            chartAssetSubmission.Id == id))!;
    }

    public async Task<bool> ChartAssetSubmissionExistsAsync(Guid id)
    {
        return await _context.ChartAssetSubmissions.AnyAsync(chartAssetSubmission => chartAssetSubmission.Id == id);
    }

    public async Task<bool> CreateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission)
    {
        await _context.ChartAssetSubmissions.AddAsync(chartAssetSubmission);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission)
    {
        _context.ChartAssetSubmissions.Update(chartAssetSubmission);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartAssetSubmissionAsync(Guid id)
    {
        _context.ChartAssetSubmissions.Remove(
            (await _context.ChartAssetSubmissions.FirstOrDefaultAsync(chartAssetSubmission =>
                chartAssetSubmission.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartAssetSubmissionsAsync(
        Expression<Func<ChartAssetSubmission, bool>>? predicate = null)
    {
        var result = _context.ChartAssetSubmissions.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}