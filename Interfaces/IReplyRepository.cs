using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IReplyRepository
{
    Task<ICollection<Reply>> GetRepliesAsync(string order, bool desc, int position, int take,
        Expression<Func<Reply, bool>>? predicate = null);

    Task<Reply> GetReplyAsync(Guid id);

    Task<bool> ReplyExistsAsync(Guid id);

    Task<bool> CreateReplyAsync(Reply reply);

    Task<bool> UpdateReplyAsync(Reply reply);

    Task<bool> RemoveReplyAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountRepliesAsync(Expression<Func<Reply, bool>>? predicate = null);
}