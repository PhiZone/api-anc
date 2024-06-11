using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ITagRepository
{
    Task<ICollection<Tag>> GetTagsAsync(List<string>? order = null, List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<Tag, bool>>? predicate = null);

    Task<Tag> GetTagAsync(Guid id);

    Task<Tag> GetTagAsync(string name);

    Task<bool> TagExistsAsync(Guid id);

    Task<bool> TagExistsAsync(string name);

    Task<bool> CreateTagAsync(Tag tag);

    Task<bool> CreateTagsAsync(List<Tag> tags);

    Task<bool> CreateTagsAsync(IEnumerable<string> tagNames);

    Task<bool> CreateTagsAsync(IEnumerable<string> tagNames, Song song);

    Task<bool> CreateTagsAsync(IEnumerable<string> tagNames, Chart chart);

    Task<bool> UpdateTagAsync(Tag tag);

    Task<bool> RemoveTagAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountTagsAsync(Expression<Func<Tag, bool>>? predicate = null);
}