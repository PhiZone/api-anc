using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IRegionRepository
{
    Task<ICollection<Region>> GetRegionsAsync(string order, bool desc, int position, int take, string? search = null,
        Expression<Func<Region, bool>>? predicate = null);

    Task<Region> GetRegionByIdAsync(int id);

    Task<Region> GetRegionAsync(string code);

    Task<ICollection<User>> GetRegionUsersByIdAsync(int id, string order, bool desc, int position, int take,
        string? search = null, Expression<Func<User, bool>>? predicate = null);

    Task<ICollection<User>> GetRegionUsersAsync(string code, string order, bool desc, int position, int take,
        string? search = null, Expression<Func<User, bool>>? predicate = null);

    Task<bool> RegionExistsByIdAsync(int id);

    Task<bool> RegionExistsAsync(string code);

    Task<bool> CreateRegionAsync(Region region);

    Task<bool> UpdateRegionAsync(Region region);

    Task<bool> DeleteRegionAsync(Region region);

    Task<bool> SaveAsync();

    Task<int> CountAsync(string? search = null, Expression<Func<Region, bool>>? predicate = null);

    Task<int> CountUsersAsync(string code, string? search = null, Expression<Func<User, bool>>? predicate = null);

    Task<int> CountUsersByIdAsync(int id, string? search = null, Expression<Func<User, bool>>? predicate = null);
}