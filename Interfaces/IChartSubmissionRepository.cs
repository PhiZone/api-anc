using System.Linq.Expressions;
using PhiZoneApi.Models;

// ReSharper disable InvertIf

namespace PhiZoneApi.Interfaces;

public interface IChartSubmissionRepository
{
    Task<ICollection<ChartSubmission>> GetChartSubmissionsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1, Expression<Func<ChartSubmission, bool>>? predicate = null, int? currentUserId = null);

    Task<ChartSubmission> GetChartSubmissionAsync(Guid id, int? currentUserId = null);

    Task<ICollection<ChartSubmission>> GetUserChartSubmissionsAsync(int userId, List<string>? order = null,
        List<bool>? desc = null,
        int? position = 0, int? take = -1, Expression<Func<ChartSubmission, bool>>? predicate = null,
        int? currentUserId = null);

    Task<bool> ChartSubmissionExistsAsync(Guid id);

    Task<bool> CreateChartSubmissionAsync(ChartSubmission chart);

    Task<bool> UpdateChartSubmissionAsync(ChartSubmission chart);

    Task<bool> RemoveChartSubmissionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartSubmissionsAsync(Expression<Func<ChartSubmission, bool>>? predicate = null);

    Task<int> CountUserChartSubmissionsAsync(int userId, Expression<Func<ChartSubmission, bool>>? predicate = null);
}