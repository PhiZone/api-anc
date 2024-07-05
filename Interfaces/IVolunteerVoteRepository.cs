using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IVolunteerVoteRepository
{
    Task<ICollection<VolunteerVote>> GetVolunteerVotesAsync(List<string>? order = null, List<bool>? desc = null,
        int? position = 0, int? take = -1,
        Expression<Func<VolunteerVote, bool>>? predicate = null);

    Task<VolunteerVote> GetVolunteerVoteAsync(Guid id);

    Task<VolunteerVote> GetVolunteerVoteAsync(Guid chartId, int userId);

    Task<bool> VolunteerVoteExistsAsync(Guid id);

    Task<bool> VolunteerVoteExistsAsync(Guid chartId, int userId);

    Task<bool> CreateVolunteerVoteAsync(VolunteerVote vote);

    Task<bool> UpdateVolunteerVoteAsync(VolunteerVote vote);

    Task<bool> RemoveVolunteerVoteAsync(Guid id);

    Task<bool> RemoveVolunteerVotesAsync(IEnumerable<VolunteerVote> votes);

    Task<bool> SaveAsync();

    Task<int> CountVolunteerVotesAsync(Expression<Func<VolunteerVote, bool>>? predicate = null);
}