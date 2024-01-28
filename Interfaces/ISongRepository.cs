using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongRepository
{
    Task<ICollection<Song>> GetSongsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Song, bool>>? predicate = null, int? currentUserId = null);

    Task<Song> GetSongAsync(Guid id, int? currentUserId = null);

    Task<Song?> GetRandomSongAsync(Expression<Func<Song, bool>>? predicate = null, int? currentUserId = null);

    Task<bool> SongExistsAsync(Guid id);

    Task<bool> CreateSongAsync(Song song);

    Task<bool> UpdateSongAsync(Song song);

    Task<bool> UpdateSongsAsync(IEnumerable<Song> songs);

    Task<bool> RemoveSongAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountSongsAsync(Expression<Func<Song, bool>>? predicate = null);

    Task<int> CountSongChartsAsync(Guid id, Expression<Func<Chart, bool>>? predicate = null);
}