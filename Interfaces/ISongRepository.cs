using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongRepository
{
    Task<ICollection<Song>> GetSongsAsync(List<string>? order = null, List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<Song, bool>>? predicate = null, int? currentUserId = null, bool? showAnonymous = false);

    Task<Song> GetSongAsync(Guid id, int? currentUserId = null);

    Task<Song?> GetRandomSongAsync(Expression<Func<Song, bool>>? predicate = null, int? currentUserId = null);

    Task<bool> SongExistsAsync(Guid id);

    Task<bool> CreateSongAsync(Song song);

    Task<bool> UpdateSongAsync(Song song);

    Task<bool> UpdateSongsAsync(IEnumerable<Song> songs);

    Task<bool> RemoveSongAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountSongsAsync(Expression<Func<Song, bool>>? predicate = null, bool? showAnonymous = false);
}