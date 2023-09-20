using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongSubmissionRepository
{
    Task<ICollection<SongSubmission>> GetSongSubmissionsAsync(List<string> order, List<bool> desc, int position,
        int take,
        string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<SongSubmission> GetSongSubmissionAsync(Guid id);

    Task<ICollection<SongSubmission>> GetUserSongSubmissionsAsync(int userId, List<string> order, List<bool> desc,
        int position,
        int take,
        string? search = null, Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<bool> SongSubmissionExistsAsync(Guid id);

    Task<bool> CreateSongSubmissionAsync(SongSubmission song);

    Task<bool> UpdateSongSubmissionAsync(SongSubmission song);

    Task<bool> RemoveSongSubmissionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountSongSubmissionsAsync(string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<int> CountUserSongSubmissionsAsync(int userId, string? search = null,
        Expression<Func<SongSubmission, bool>>? predicate = null);
}