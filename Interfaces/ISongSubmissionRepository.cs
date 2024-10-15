using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface ISongSubmissionRepository
{
    Task<ICollection<SongSubmission>> GetSongSubmissionsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<SongSubmission> GetSongSubmissionAsync(Guid id);

    Task<ICollection<SongSubmission>> GetUserSongSubmissionsAsync(int userId, List<string>? order = null,
        List<bool>? desc = null, int? position = 0, int? take = -1,
        Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<bool> SongSubmissionExistsAsync(Guid id);

    Task<bool> CreateSongSubmissionAsync(SongSubmission song);

    Task<bool> UpdateSongSubmissionAsync(SongSubmission song);

    Task<bool> UpdateSongSubmissionsAsync(IEnumerable<SongSubmission> songs);

    Task<bool> RemoveSongSubmissionAsync(Guid id);

    Task<bool> RemoveSongSubmissionsAsync(IEnumerable<SongSubmission> songs);

    Task<bool> SaveAsync();

    Task<int> CountSongSubmissionsAsync(Expression<Func<SongSubmission, bool>>? predicate = null);

    Task<int> CountUserSongSubmissionsAsync(int userId, Expression<Func<SongSubmission, bool>>? predicate = null);
}