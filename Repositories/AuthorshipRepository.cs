using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class AuthorshipRepository(ApplicationDbContext context) : IAuthorshipRepository
{
    public async Task<ICollection<Authorship>> GetAuthorshipsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<Authorship, bool>>? predicate = null,
        int? currentUserId = null)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        var result = query.Include(e => e.Author).ThenInclude(e => e.Region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Author)
                .ThenInclude(e => e.FollowerRelations.Where(relation =>
                        relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                    .Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Authorship> GetAuthorshipAsync(Guid resourceId, int authorId, int? currentUserId = null)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        var result = query.Include(e => e.Author).ThenInclude(e => e.Region).AsQueryable();
        if (currentUserId != null)
            result = result.Include(e => e.Author)
                .ThenInclude(e => e.FollowerRelations.Where(relation =>
                        relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                    .Take(1));
        return (await result.FirstOrDefaultAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId))!;
    }

    public async Task<Authorship> GetAuthorshipAsync(Guid id, int? currentUserId = null)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        var result = query.Include(e => e.Author).ThenInclude(e => e.Region).AsQueryable();
        if (currentUserId != null)
            result = result.Include(e => e.Author)
                .ThenInclude(e => e.FollowerRelations.Where(relation =>
                        relation.FollowerId == currentUserId && relation.Type != UserRelationType.Blacklisted)
                    .Take(1));
        return (await result.FirstOrDefaultAsync(authorship => authorship.Id == id))!;
    }

    public async Task<bool> CreateAuthorshipAsync(Authorship authorship)
    {
        await context.Authorships.AddAsync(authorship);
        return await SaveAsync();
    }

    public async Task<bool> UpdateAuthorshipAsync(Authorship authorship)
    {
        context.Authorships.Update(authorship);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAuthorshipAsync(Guid resourceId, int authorId)
    {
        context.Authorships.Remove((await context.Authorships.FirstOrDefaultAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId))!);
        return await SaveAsync();
    }

    public async Task<bool> RemoveAuthorshipAsync(Guid id)
    {
        context.Authorships.Remove((await context.Authorships.FirstOrDefaultAsync(authorship => authorship.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountAuthorshipsAsync(Expression<Func<Authorship, bool>>? predicate = null)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        return await (predicate != null ? query.CountAsync(predicate) : query.CountAsync());
    }

    public async Task<bool> AuthorshipExistsAsync(Guid resourceId, int authorId)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        return await query.AnyAsync(authorship =>
            authorship.ResourceId == resourceId && authorship.AuthorId == authorId &&
            !authorship.Resource.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value));
    }

    public async Task<bool> AuthorshipExistsAsync(Guid id)
    {
        var query = context.Authorships
            .Where(authorship => !authorship.Resource.EventPresences.Any(e =>
                e.IsAnonymous != null && e.IsAnonymous.Value))
            .Union(context.Authorships.OfType<Authorship>()
                .Where(authorship => authorship.Resource is Song)
                .Select(authorship => new { Authorship = authorship, Song = authorship.Resource as Song })
                .Where(a => !a.Song!.Charts.Any(c =>
                    c.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value)))
                .Select(a => a.Authorship));
        return await query.AnyAsync(authorship =>
            authorship.Id == id &&
            !authorship.Resource.EventPresences.Any(e => e.IsAnonymous != null && e.IsAnonymous.Value));
    }
}