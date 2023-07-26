using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IVolunteerVoteRepository
{
    Task<ICollection<VolunteerVote>> GetVolunteerVotesAsync(string order, bool desc, int position, int take,
        Expression<Func<VolunteerVote, bool>>? predicate = null);

    Task<VolunteerVote> GetVolunteerVoteAsync(Guid id);

    Task<VolunteerVote> GetVolunteerVoteAsync(Guid chartId, int userId);

    Task<bool> VolunteerVoteExistsAsync(Guid id);

    Task<bool> VolunteerVoteExistsAsync(Guid chartId, int userId);

    Task<bool> CreateVolunteerVoteAsync(VolunteerVote vote);

    Task<bool> RemoveVolunteerVoteAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountVolunteerVotesAsync(Expression<Func<VolunteerVote, bool>>? predicate = null);
}