using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ReplyRepository(ApplicationDbContext context) : IReplyRepository
{
    public async Task<ICollection<Reply>> GetRepliesAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Reply, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Replies.Include(e => e.Owner).ThenInclude(e => e.Region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Reply> GetReplyAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Reply> result = context.Replies.Include(e => e.Owner).ThenInclude(e => e.Region);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<bool> ReplyExistsAsync(Guid id)
    {
        return await context.Replies.AnyAsync(like => like.Id == id);
    }

    public async Task<bool> CreateReplyAsync(Reply reply)
    {
        var comment = await context.Comments.FirstAsync(e => e.Id == reply.CommentId);
        comment.ReplyCount = await context.Replies.CountAsync(e => e.CommentId == reply.CommentId) + 1;
        await context.Replies.AddAsync(reply);
        return await SaveAsync();
    }

    public async Task<bool> UpdateReplyAsync(Reply reply)
    {
        context.Replies.Update(reply);
        return await SaveAsync();
    }

    public async Task<bool> RemoveReplyAsync(Guid id)
    {
        var reply = await context.Replies.Include(e => e.Comment).FirstAsync(reply => reply.Id == id);
        reply.Comment.ReplyCount = await context.Replies.CountAsync(e => e.CommentId == reply.CommentId) - 1;
        context.Replies.Remove(reply);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountRepliesAsync(Expression<Func<Reply, bool>>? predicate = null)
    {
        var result = context.Replies.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}