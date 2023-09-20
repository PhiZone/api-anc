using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongRepository
{
    Task<ICollection<Song>> GetSongsAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null,
        Expression<Func<Song, bool>>? predicate = null);

    Task<Song> GetSongAsync(Guid id);

    Task<Song?> GetRandomSongAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null);

    Task<ICollection<Chart>> GetSongChartsAsync(Guid id, List<string> order, List<bool> desc, int position, int take,
        string? search = null, Expression<Func<Chart, bool>>? predicate = null);

    Task<bool> SongExistsAsync(Guid id);

    Task<bool> CreateSongAsync(Song song);

    Task<bool> UpdateSongAsync(Song song);

    Task<bool> UpdateSongsAsync(IEnumerable<Song> songs);

    Task<bool> RemoveSongAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountSongsAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null);

    Task<int> CountSongChartsAsync(Guid id, string? search = null, Expression<Func<Chart, bool>>? predicate = null);
}