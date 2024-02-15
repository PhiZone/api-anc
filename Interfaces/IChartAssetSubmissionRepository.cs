using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IChartAssetSubmissionRepository
{
    Task<ICollection<ChartAssetSubmission>> GetChartAssetSubmissionsAsync(List<string> order, List<bool> desc,
        int position,
        int take,
        Expression<Func<ChartAssetSubmission, bool>>? predicate = null);

    Task<ChartAssetSubmission> GetChartAssetSubmissionAsync(Guid id);

    Task<bool> ChartAssetSubmissionExistsAsync(Guid id);

    Task<bool> CreateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission);

    Task<bool> UpdateChartAssetSubmissionAsync(ChartAssetSubmission chartAssetSubmission);

    Task<bool> RemoveChartAssetSubmissionAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountChartAssetSubmissionsAsync(Expression<Func<ChartAssetSubmission, bool>>? predicate = null);
}