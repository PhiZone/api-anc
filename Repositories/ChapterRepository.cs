using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
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
            search = search.Trim().ToUpper();
            result = result.Where(chapter =>
                chapter.Title.ToUpper().Contains(search) || chapter.Subtitle.ToUpper().Contains(search) ||
                (chapter.Description != null && chapter.Description.ToUpper().Contains(search)));
        }

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<Chapter> GetChapterAsync(Guid id)
    {
        return (await _context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
    }

    public async Task<ICollection<Song>> GetChapterSongsAsync(Guid id, string order, bool desc, int position,
        int take, string? search = null, Expression<Func<Song, bool>>? predicate = null)
    {
        var chapter = (await _context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
        var result = chapter.Songs.AsQueryable().OrderBy(order, desc);

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(song => song.Title.ToUpper().Contains(search) ||
                                          (song.Edition != null && song.Edition.ToUpper().Contains(search)) ||
                                          song.AuthorName.ToUpper().Contains(search) || (song.Description != null &&
                                              song.Description.ToUpper().Contains(search)));
        }

        return await result.Skip(position).Take(take).ToListAsync();
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
            search = search.Trim().ToUpper();
            result = result.Where(chapter =>
                chapter.Title.ToUpper().Contains(search) || chapter.Subtitle.ToUpper().Contains(search) ||
                (chapter.Description != null && chapter.Description.ToUpper().Contains(search)));
        }

        return await result.CountAsync();
    }

    public async Task<int> CountSongsAsync(Guid id, string? search = null,
        Expression<Func<Song, bool>>? predicate = null)
    {
        var chapter = (await _context.Chapters.FirstOrDefaultAsync(chapter => chapter.Id == id))!;
        var result = chapter.Songs.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        if (search != null)
        {
            search = search.Trim().ToUpper();
            result = result.Where(song => song.Title.ToUpper().Contains(search) ||
                                          (song.Edition != null && song.Edition.ToUpper().Contains(search)) ||
                                          song.AuthorName.ToUpper().Contains(search) || (song.Description != null &&
                                              song.Description.ToUpper().Contains(search)));
        }

        return await result.CountAsync();
    }
}