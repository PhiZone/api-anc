using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface IChartRepository
{
    Task<ICollection<Chart>> GetChartsAsync(List<string> order, List<bool> desc, int position, int take, string? search = null,
        Expression<Func<Chart, bool>>? predicate = null);

    Task<Chart> GetChartAsync(Guid id);

    Task<Chart?> GetRandomChartAsync(string? search = null, Expression<Func<Chart, bool>>? predicate = null);

    Task<ICollection<Record>> GetChartRecordsAsync(Guid id, List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Record, bool>>? predicate = null);

    Task<bool> ChartExistsAsync(Guid id);

    Task<bool> CreateChartAsync(Chart chart);

    Task<bool> UpdateChartAsync(Chart chart);

    Task<bool> UpdateChartsAsync(IEnumerable<Chart> charts);

    Task<bool> RemoveChartAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartsAsync(string? search = null, Expression<Func<Chart, bool>>? predicate = null);

    Task<int> CountChartRecordsAsync(Guid id, Expression<Func<Record, bool>>? predicate = null);
}