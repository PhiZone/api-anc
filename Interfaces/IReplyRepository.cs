using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IReplyRepository
{
    Task<ICollection<Reply>> GetRepliesAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Reply, bool>>? predicate = null, int? currentUserId = null);

    Task<Reply> GetReplyAsync(Guid id, int? currentUserId = null);

    Task<bool> ReplyExistsAsync(Guid id);

    Task<bool> CreateReplyAsync(Reply reply);

    Task<bool> UpdateReplyAsync(Reply reply);

    Task<bool> RemoveReplyAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountRepliesAsync(Expression<Func<Reply, bool>>? predicate = null);
}