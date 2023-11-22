using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChapterRepository(ApplicationDbContext context) : IChapterRepository
{
    public async Task<ICollection<Chapter>> GetChaptersAsync(List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<Chapter, bool>>? predicate = null)
    {
        var result = context.Chapters.OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chapter =>
                EF.Functions.Like(chapter.Title.ToUpper(), search) ||
                EF.Functions.Like(chapter.Subtitle.ToUpper(), search) ||
                (chapter.Description != null && EF.Functions.Like(chapter.Description.ToUpper(), search)));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Chapter> GetChapterAsync(Guid id)
    {
        return (await context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
    }

    public async Task<ICollection<Admission>> GetChapterSongsAsync(Guid id, List<string> order, List<bool> desc,
        int position,
        int take, string? search = null, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions
            .Where(admission => admission.AdmitterId == id && admission.Status == RequestStatus.Approved)
            .OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(admission =>
                admission.Label != null && EF.Functions.Like(admission.Label.ToUpper(), search));
        }

        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> ChapterExistsAsync(Guid id)
    {
        return (await context.Chapters.AnyAsync(chapter => chapter.Id == id))!;
    }

    public async Task<bool> CreateChapterAsync(Chapter chapter)
    {
        await context.Chapters.AddAsync(chapter);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChapterAsync(Chapter chapter)
    {
        context.Chapters.Update(chapter);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChapterAsync(Guid id)
    {
        context.Chapters.Remove((await context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChaptersAsync(string? search = null, Expression<Func<Chapter, bool>>? predicate = null)
    {
        var result = context.Chapters.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(chapter =>
                EF.Functions.Like(chapter.Title.ToUpper(), search) ||
                EF.Functions.Like(chapter.Subtitle.ToUpper(), search) ||
                (chapter.Description != null && EF.Functions.Like(chapter.Description.ToUpper(), search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountChapterSongsAsync(Guid id, string? search = null,
        Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = context.Admissions.Where(admission =>
            admission.AdmitterId == id && admission.Status == RequestStatus.Approved);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = $"%{search.Trim().ToUpper()}%";
            result = result.Where(admission =>
                admission.Label != null && EF.Functions.Like(admission.Label.ToUpper(), search));
        }

        return await result.CountAsync();
    }
}