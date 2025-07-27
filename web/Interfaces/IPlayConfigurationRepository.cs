using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IPlayConfigurationRepository
{
    Task<ICollection<PlayConfiguration>> GetPlayConfigurationsAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0,
        int? take = -1,
        Expression<Func<PlayConfiguration, bool>>? predicate = null);

    Task<PlayConfiguration> GetPlayConfigurationAsync(Guid id);

    Task<bool> PlayConfigurationExistsAsync(Guid id);

    Task<bool> CreatePlayConfigurationAsync(PlayConfiguration configuration);

    Task<bool> UpdatePlayConfigurationAsync(PlayConfiguration configuration);

    Task<bool> RemovePlayConfigurationAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountPlayConfigurationsAsync(
        Expression<Func<PlayConfiguration, bool>>? predicate = null);
}