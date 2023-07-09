using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IChapterRepository
{
    Task<ICollection<Chapter>> GetChaptersAsync(string order, bool desc, int position, int take, string? search = null,
        Expression<Func<Chapter, bool>>? predicate = null);

    Task<Chapter> GetChapterAsync(Guid id);

    Task<ICollection<Song>> GetChapterSongsAsync(Guid id, string order, bool desc, int position, int take,
        string? search = null, Expression<Func<Song, bool>>? predicate = null);

    Task<bool> ChapterExistsAsync(Guid id);

    Task<bool> CreateChapterAsync(Chapter chapter);

    Task<bool> UpdateChapterAsync(Chapter chapter);

    Task<bool> RemoveChapterAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAsync(string? search = null, Expression<Func<Chapter, bool>>? predicate = null);

    Task<int> CountSongsAsync(Guid id, string? search = null, Expression<Func<Song, bool>>? predicate = null);
}