using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IRegionRepository
{
    Task<ICollection<Region>> GetRegionsAsync(string order, bool desc, int position, int take);

    Task<Region> GetRegionByIdAsync(int id);

    Task<Region> GetRegionAsync(string code);

    Task<ICollection<User>> GetRegionUsersByIdAsync(int id, string order, bool desc, int position, int take);

    Task<ICollection<User>> GetRegionUsersAsync(string code, string order, bool desc, int position, int take);

    Task<bool> RegionExistsByIdAsync(int id);

    Task<bool> RegionExistsAsync(string code);

    Task<bool> CreateRegionAsync(Region region);

    Task<bool> UpdateRegionAsync(Region region);

    Task<bool> DeleteRegionAsync(Region region);

    Task<bool> SaveAsync();

    Task<int> CountAsync();

    Task<int> CountUsersAsync(string code);

    Task<int> CountUsersByIdAsync(int id);
}