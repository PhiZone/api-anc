using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongRepository
{
    Task<ICollection<Song>> GetSongsAsync(string order, bool desc, int position, int take, string? search = null,
        Expression<Func<Song, bool>>? predicate = null);

    Task<Song> GetSongAsync(Guid id);

    Task<ICollection<Chart>> GetSongChartsAsync(Guid id, string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Chart, bool>>? predicate = null);

    Task<bool> SongExistsAsync(Guid id);

    Task<bool> CreateSongAsync(Song song);

    Task<bool> UpdateSongAsync(Song song);
    
    Task<bool> RemoveSongAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAsync(string? search = null, Expression<Func<Song, bool>>? predicate = null);

    Task<int> CountChartsAsync(Guid id, string? search = null, Expression<Func<Chart, bool>>? predicate = null);
}