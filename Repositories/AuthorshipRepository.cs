using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class AuthorshipRepository : IAuthorshipRepository
{
    private readonly ApplicationDbContext _context;

    public AuthorshipRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ICollection<Authorship>> GetResourcesAsync(int authorId, string order, bool desc, int position,
        int take, Expression<Func<Authorship, bool>>? predicate = null)
    {
        var result = _context.Authorships.Where(authorship => authorship.AuthorId == authorId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<Authorship>> GetAuthorsAsync(Guid resourceId, string order, bool desc, int position,
        int take, Expression<Func<Authorship, bool>>? predicate = null)
    {
        var result = _context.Authorships.Where(authorship => authorship.ResourceId == resourceId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);

        return await result.Skip(position).Take(take).ToListAsync();
    }

    public async Task<ICollection<Authorship>> GetAuthorshipsAsync(string order, bool desc, int position, int take,
        Expression<Func<Authorship, bool>>? predicate = null)
    {
        var result = _context.Authorships.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Authorship> GetAuthorshipAsync(Guid resourceId, int authorId)
    {
        return (await _context.Authorships.FirstOrDefaultAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId))!;
    }

    public async Task<bool> CreateAuthorshipAsync(Authorship authorship)
    {
        await _context.Authorships.AddAsync(authorship);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAuthorshipAsync(Authorship authorship)
    {
        _context.Authorships.Update(authorship);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAuthorshipAsync(Guid resourceId, int authorId)
    {
        _context.Authorships.Remove((await _context.Authorships.FirstOrDefaultAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await _context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAuthorshipsAsync(Expression<Func<Authorship, bool>>? predicate = null)
    {
        if (predicate != null) return await _context.Authorships.Where(predicate).CountAsync();
        return await _context.Authorships.CountAsync();
    }

    public async Task<bool> AuthorshipExistsAsync(Guid resourceId, int authorId)
    {
        return await _context.Authorships.AnyAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId);
    }

    public async Task<int> CountResourcesAsync(int authorId, Expression<Func<Authorship, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.Authorships.Where(authorship => authorship.Author.Id == authorId)
                .Where(predicate)
                .CountAsync();

        return await _context.Authorships.Where(authorship => authorship.Author.Id == authorId).CountAsync();
    }

    public async Task<int> CountAuthorsAsync(Guid resourceId, Expression<Func<Authorship, bool>>? predicate = null)
    {
        if (predicate != null)
            return await _context.Authorships.Where(authorship => authorship.Resource.Id == resourceId)
                .Where(predicate)
                .CountAsync();

        return await _context.Authorships.Where(authorship => authorship.Resource.Id == resourceId).CountAsync();
    }
}