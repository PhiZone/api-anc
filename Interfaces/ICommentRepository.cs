using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface ICommentRepository
{
    Task<ICollection<Comment>> GetCommentsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Comment, bool>>? predicate = null, int? currentUserId = null);

    Task<Comment> GetCommentAsync(Guid id, int? currentUserId = null);

    Task<ICollection<Reply>> GetCommentRepliesAsync(Guid id, List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Reply, bool>>? predicate = null, int? currentUserId = null);

    Task<bool> CommentExistsAsync(Guid id);

    Task<bool> CreateCommentAsync(Comment comment);

    Task<bool> UpdateCommentAsync(Comment comment);

    Task<bool> RemoveCommentAsync(Guid id);

    Task<bool> SaveAsync();

    Task<int> CountCommentsAsync(Expression<Func<Comment, bool>>? predicate = null);

    Task<int> CountCommentRepliesAsync(Guid id, Expression<Func<Reply, bool>>? predicate = null);
}