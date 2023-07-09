using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface IChartRepository
{
    Task<ICollection<Chart>> GetChartsAsync(string order, bool desc, int position, int take, string? search = null,
        Expression<Func<Chart, bool>>? predicate = null);

    Task<Chart> GetChartAsync(Guid id);

    Task<ICollection<Record>> GetChartRecordsAsync(Guid id, string order, bool desc, int position, int take, Expression<Func<Record, bool>>? predicate = null);

    Task<bool> ChartExistsAsync(Guid id);

    Task<bool> CreateChartAsync(Chart song);

    Task<bool> UpdateChartAsync(Chart song);
    
    Task<bool> RemoveChartAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountAsync(string? search = null, Expression<Func<Chart, bool>>? predicate = null);

    Task<int> CountRecordsAsync(Guid id, string? search = null, Expression<Func<Record, bool>>? predicate = null);
}