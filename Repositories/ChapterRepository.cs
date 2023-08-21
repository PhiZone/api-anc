using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class ChapterRepository : IChapterRepository
{
    private readonly ApplicationDbContext _context;

    public ChapterRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Chapter>> GetChaptersAsync(string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Chapter, bool>>? predicate = null)
    {
        var result = _context.Chapters.OrderBy(order, desc);

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
        return (await _context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
    }

    public async Task<ICollection<Admission>> GetChapterSongsAsync(Guid id, string order, bool desc, int position,
        int take, string? search = null, Expression<Func<Admission, bool>>? predicate = null)
    {
        var result = _context.Admissions
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
        return (await _context.Chapters.AnyAsync(chapter => chapter.Id == id))!;
    }

    public async Task<bool> CreateChapterAsync(Chapter chapter)
    {
        await _context.Chapters.AddAsync(chapter);
        return await SaveAsync();
    }

    public async Task<bool> UpdateChapterAsync(Chapter chapter)
    {
        _context.Chapters.Update(chapter);
        return await SaveAsync();
    }

    public async Task<bool> RemoveChapterAsync(Guid id)
    {
        _context.Chapters.Remove((await _context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountChaptersAsync(string? search = null, Expression<Func<Chapter, bool>>? predicate = null)
    {
        var result = _context.Chapters.AsQueryable();

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
        var result = _context.Admissions.Where(admission =>
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