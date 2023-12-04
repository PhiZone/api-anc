using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IRegionRepository
{
    Task<ICollection<Region>> GetRegionsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Region, bool>>? predicate = null);

    Task<Region> GetRegionAsync(int id);

    Task<Region> GetRegionAsync(string code);

    Task<ICollection<User>> GetRegionUsersAsync(int id, List<string> order, List<bool> desc, int position, int take,
        Expression<Func<User, bool>>? predicate = null);

    Task<ICollection<User>> GetRegionUsersAsync(string code, List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<User, bool>>? predicate = null);

    Task<bool> RegionExistsAsync(int id);

    Task<bool> RegionExistsAsync(string code);

    bool RegionExists(string code);

    Task<bool> CreateRegionAsync(Region region);

    Task<bool> UpdateRegionAsync(Region region);

    Task<bool> RemoveRegionAsync(string code);

    Task<bool> RemoveRegionAsync(int id);

    Task<bool> SaveAsync();

    Task<int> CountRegionsAsync(Expression<Func<Region, bool>>? predicate = null);

    Task<int> CountRegionUsersAsync(string code, Expression<Func<User, bool>>? predicate = null);

    Task<int> CountRegionUsersAsync(int id, Expression<Func<User, bool>>? predicate = null);
}