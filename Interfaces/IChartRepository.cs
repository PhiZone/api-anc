using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface IChartRepository
{
    Task<ICollection<Chart>> GetChartsAsync(List<string>? order = null, List<bool>? desc = null, int? position = 0,
        int? take = -1,
        Expression<Func<Chart, bool>>? predicate = null, int? currentUserId = null);

    Task<Chart> GetChartAsync(Guid id, int? currentUserId = null, bool includeAssets = false);

    Task<Chart?> GetRandomChartAsync(Expression<Func<Chart, bool>>? predicate = null, int? currentUserId = null);

    Task<bool> ChartExistsAsync(Guid id);

    Task<bool> CreateChartAsync(Chart chart);

    Task<bool> UpdateChartAsync(Chart chart);

    Task<bool> UpdateChartsAsync(IEnumerable<Chart> charts);

    Task<bool> RemoveChartAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartsAsync(Expression<Func<Chart, bool>>? predicate = null);
}