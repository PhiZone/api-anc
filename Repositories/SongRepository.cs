using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

// ReSharper disable InvertIf

namespace PhiZoneApi.Repositories;

public class SongRepository(ApplicationDbContext context, IMeilisearchService meilisearchService) : ISongRepository
{
    public async Task<ICollection<Song>> GetSongsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<Song, bool>>? predicate = null, int? currentUserId = null,
        bool? showAnonymous = false)
    {
        var result = context.Songs
            .Where(e => (showAnonymous != null && showAnonymous.Value) ||
                        (!e.EventPresences.Any(f =>
                             f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value) &&
                         !e.Charts.Any(f => f.EventPresences.Any(g =>
                             g.Type == EventResourceType.Entry && g.IsAnonymous != null && g.IsAnonymous.Value &&
                             g.Team != null && g.Team.Participants.Any(h => h.Id == e.OwnerId)))))
            .Include(e => e.Charts)
            .ThenInclude(e => e.EventPresences)
            .ThenInclude(e => e.Team)
            .ThenInclude(e => e!.Participations)
            .Include(e => e.Tags)
            .Include(e => e.EventPresences)
            .ThenInclude(e => e.Team)
            .OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position ?? 0);
        return take >= 0 ? await result.Take(take.Value).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Song> GetSongAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Song> result = context.Songs.Include(e => e.Charts)
            .ThenInclude(e => e.EventPresences)
            .ThenInclude(e => e.Team)
            .ThenInclude(e => e!.Participations)
            .Include(e => e.Tags)
            .Include(e => e.EventPresences)
            .ThenInclude(e => e.Team);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(song => song.Id == id))!;
    }

    public async Task<Song?> GetRandomSongAsync(Expression<Func<Song, bool>>? predicate = null,
        int? currentUserId = null)
    {
        IQueryable<Song> result = context.Songs
            .Where(e =>
                (!e.EventPresences.Any(f =>
                     f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value) &&
                 !e.Charts.Any(f => f.EventPresences.Any(g =>
                     g.Type == EventResourceType.Entry && g.IsAnonymous != null && g.IsAnonymous.Value &&
                     g.Team != null && g.Team.Participants.Any(h => h.Id == e.OwnerId)))))
            .Include(e => e.Charts)
            .ThenInclude(e => e.EventPresences)
            .ThenInclude(e => e.Team)
            .ThenInclude(e => e!.Participations)
            .Include(e => e.Tags)
            .Include(e => e.EventPresences)
            .OrderBy(song => EF.Functions.Random());
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return await result.FirstOrDefaultAsync();
    }

    public async Task<bool> SongExistsAsync(Guid id)
    {
        return (await context.Songs.AnyAsync(song => song.Id == id))!;
    }

    public async Task<bool> CreateSongAsync(Song song)
    {
        await context.Songs.AddAsync(song);
        await meilisearchService.AddAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongAsync(Song song)
    {
        context.Songs.Update(song);
        await meilisearchService.UpdateAsync(song);
        return await SaveAsync();
    }

    public async Task<bool> UpdateSongsAsync(IEnumerable<Song> songs)
    {
        var enumerable = songs.ToList();
        context.Songs.UpdateRange(enumerable);
        await meilisearchService.UpdateBatchAsync(enumerable);
        return await SaveAsync();
    }

    public async Task<bool> RemoveSongAsync(Guid id)
    {
        context.Songs.Remove((await context.Songs.FirstOrDefaultAsync(song => song.Id == id))!);
        await meilisearchService.DeleteAsync<Song>(id);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountSongsAsync(Expression<Func<Song, bool>>? predicate = null, bool? showAnonymous = false)
    {
        var result = context.Songs.Where(e =>
            (showAnonymous != null && showAnonymous.Value) ||
            (!e.EventPresences.Any(f =>
                f.Type == EventResourceType.Entry && f.IsAnonymous != null && f.IsAnonymous.Value) && !e.Charts.Any(f =>
                f.EventPresences.Any(g =>
                    g.Type == EventResourceType.Entry && g.IsAnonymous != null && g.IsAnonymous.Value &&
                    g.Team != null && g.Team.Participants.Any(h => h.Id == e.OwnerId)))));
        if (predicate != null) result = result.Where(predicate);
        return await result.CountAsync();
    }
}