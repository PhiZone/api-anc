using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChapterRepository(ApplicationDbContext context, IMeilisearchService meilisearchService)
    : IChapterRepository
{
    public async Task<ICollection<Chapter>> GetChaptersAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<Chapter, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Chapters.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Chapter> GetChapterAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Chapter> result = context.Chapters;
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
    }

    public async Task<ICollection<Admission>> GetChapterSongsAsync(Guid id, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions
            .Where(admission => admission.AdmitterId == id && admission.Status == RequestStatus.Approved)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> ChapterExistsAsync(Guid id)
    {
        return (await context.Chapters.AnyAsync(chapter => chapter.Id == id))!;
    }

    public async Task<bool> CreateChapterAsync(Chapter chapter)
    {
        await context.Chapters.AddAsync(chapter);
        await meilisearchService.AddAsync(chapter);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChapterAsync(Chapter chapter)
    {
        context.Chapters.Update(chapter);
        await meilisearchService.UpdateAsync(chapter);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChapterAsync(Guid id)
    {
        context.Chapters.Remove((await context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!);
        await meilisearchService.DeleteAsync<Chapter>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChaptersAsync(Expression<Func<Chapter, bool>>? predicate = null)
    {
        var result = context.Chapters.AsQueryable();
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }

    public async Task<int> CountChapterSongsAsync(Guid id, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions.Where(admission =>
            admission.AdmitterId == id && admission.Status == RequestStatus.Approved);
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}