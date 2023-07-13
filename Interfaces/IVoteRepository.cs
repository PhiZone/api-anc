using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IVoteRepository
{
    Task<ICollection<Vote>> GetVotesAsync(string order, bool desc, int position, int take,
        Expression<Func<Vote, bool>>? predicate = null);

    Task<Vote> GetVoteAsync(Guid id);

    Task<Vote> GetVoteAsync(Guid chartId, int userId);

    Task<bool> VoteExistsAsync(Guid id);

    Task<bool> VoteExistsAsync(Guid chartId, int userId);

    Task<bool> CreateVoteAsync(Vote vote);

    Task<bool> RemoveVoteAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountVotesAsync(Expression<Func<Vote, bool>>? predicate = null);
}