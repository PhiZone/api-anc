using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IChapterRepository
{
    Task<ICollection<Chapter>> GetChaptersAsync(List<string> order, List<bool> desc, int position, int take,
        string? search = null,
        Expression<Func<Chapter, bool>>? predicate = null);

    Task<Chapter> GetChapterAsync(Guid id);

    Task<ICollection<Admission>> GetChapterSongsAsync(Guid id, List<string> order, List<bool> desc, int position,
        int take,
        string? search = null, Expression<Func<Admission, bool>>? predicate = null);

    Task<bool> ChapterExistsAsync(Guid id);

    Task<bool> CreateChapterAsync(Chapter chapter);

    Task<bool> UpdateChapterAsync(Chapter chapter);

    Task<bool> RemoveChapterAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChaptersAsync(string? search = null, Expression<Func<Chapter, bool>>? predicate = null);

    Task<int> CountChapterSongsAsync(Guid id, string? search = null,
        Expression<Func<Admission, bool>>? predicate = null);
}