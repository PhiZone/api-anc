using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface IChartSubmissionRepository
{
    Task<ICollection<ChartSubmission>> GetChartSubmissionsAsync(string order, bool desc, int position, int take,
        string? search = null,
        Expression<Func<ChartSubmission, bool>>? predicate = null);

    Task<ChartSubmission> GetChartSubmissionAsync(Guid id);

    Task<ICollection<ChartSubmission>> GetUserChartSubmissionsAsync(int userId, string order, bool desc, int position,
        int take,
        string? search = null, Expression<Func<ChartSubmission, bool>>? predicate = null);

    Task<bool> ChartSubmissionExistsAsync(Guid id);

    Task<bool> CreateChartSubmissionAsync(ChartSubmission chart);

    Task<bool> UpdateChartSubmissionAsync(ChartSubmission chart);

    Task<bool> RemoveChartSubmissionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartSubmissionsAsync(string? search = null,
        Expression<Func<ChartSubmission, bool>>? predicate = null);

    Task<int> CountUserChartSubmissionsAsync(int userId, string? search = null,
        Expression<Func<ChartSubmission, bool>>? predicate = null);
}