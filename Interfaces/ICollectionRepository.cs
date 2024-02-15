using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ICollectionRepository
{
    Task<ICollection<Collection>> GetCollectionsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Collection, bool>>? predicate = null, int? currentUserId = null);

    Task<Collection> GetCollectionAsync(Guid id, int? currentUserId = null);

    Task<ICollection<Admission>> GetCollectionChartsAsync(Guid id, List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Admission, bool>>? predicate = null);

    Task<bool> CollectionExistsAsync(Guid id);

    Task<bool> CreateCollectionAsync(Collection chapter);

    Task<bool> UpdateCollectionAsync(Collection chapter);

    Task<bool> RemoveCollectionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountCollectionsAsync(Expression<Func<Collection, bool>>? predicate = null);

    Task<int> CountCollectionChartsAsync(Guid id, Expression<Func<Admission, bool>>? predicate = null);
}