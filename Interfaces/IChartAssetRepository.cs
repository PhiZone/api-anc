using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IChartAssetRepository
{
    Task<ICollection<ChartAsset>> GetChartAssetsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<ChartAsset, bool>>? predicate = null);

    Task<ChartAsset> GetChartAssetAsync(Guid id);

    Task<bool> ChartAssetExistsAsync(Guid id);

    Task<bool> CreateChartAssetAsync(ChartAsset chartAsset);

    Task<bool> UpdateChartAssetAsync(ChartAsset chartAsset);

    Task<bool> RemoveChartAssetAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartAssetsAsync(Expression<Func<ChartAsset, bool>>? predicate = null);
}