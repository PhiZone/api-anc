using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChartSubmissionRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IChartSubmissionRepository
{
    public async Task<ICollection<ChartSubmission>> GetChartSubmissionsAsync(List<string> order, List<bool> desc,
        int position, int take, Expression<Func<ChartSubmission, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.ChartSubmissions.Include(e => e.Song)
            .ThenInclude(e => e!.Charts)
            .Include(e => e.Song)
            .ThenInclude(e => e!.Tags)
            .Include(e => e.SongSubmission)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.VolunteerVotes.Where(vote => vote.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<ChartSubmission>> GetUserChartSubmissionsAsync(int userId, List<string> order,
        List<bool> desc, int position, int take, Expression<Func<ChartSubmission, bool>>? predicate = null,
        int? currentUserId = null)
    {
        var result = context.ChartSubmissions.Include(e => e.Song)
            .ThenInclude(e => e!.Charts)
            .Include(e => e.Song)
            .ThenInclude(e => e!.Tags)
            .Include(e => e.SongSubmission)
            .Where(chart => chart.OwnerId == userId)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.VolunteerVotes.Where(vote => vote.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ChartSubmission> GetChartSubmissionAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<ChartSubmission> result = context.ChartSubmissions.Include(e => e.Song)
            .ThenInclude(e => e!.Charts)
            .Include(e => e.Song)
            .ThenInclude(e => e!.Tags)
            .Include(e => e.SongSubmission);
        if (currentUserId != null)
            result = result.Include(e => e.VolunteerVotes.Where(vote => vote.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> ChartSubmissionExistsAsync(Guid id)
    {
        return (await context.ChartSubmissions.AnyAsync(chart => chart.Id == id))!;
    }

    public async Task<bool> CreateChartSubmissionAsync(ChartSubmission chart)
    {
        await context.ChartSubmissions.AddAsync(chart);
        await meilisearchService.AddAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChartSubmissionAsync(ChartSubmission chart)
    {
        context.ChartSubmissions.Update(chart);
        await meilisearchService.UpdateAsync(chart);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChartSubmissionAsync(Guid id)
    {
        context.ChartSubmissions.Remove((await context.ChartSubmissions.FirstOrDefaultAsync(chart => chart.Id == id))!);
        await meilisearchService.DeleteAsync<ChartSubmission>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChartSubmissionsAsync(Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = context.ChartSubmissions.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountUserChartSubmissionsAsync(int userId,
        Expression<Func<ChartSubmission, bool>>? predicate = null)
    {
        var result = context.ChartSubmissions.Where(chart => chart.OwnerId == userId).AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}