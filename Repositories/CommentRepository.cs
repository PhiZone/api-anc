using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class CommentRepository(ApplicationDbContext context) : ICommentRepository
{
    public async Task<ICollection<Comment>> GetCommentsAsync(List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Comment, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Comments.Include(e => e.Owner).ThenInclude(e => e.Region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Comment> GetCommentAsync(Guid id, int? currentUserId = null)
    {
        IQueryable<Comment> result = context.Comments.Include(e => e.Owner).ThenInclude(e => e.Region);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        return (await result.FirstOrDefaultAsync(like => like.Id == id))!;
    }

    public async Task<ICollection<Reply>> GetCommentRepliesAsync(Guid id, List<string> order, List<bool> desc,
        int position, int take, Expression<Func<Reply, bool>>? predicate = null, int? currentUserId = null)
    {
        var result = context.Replies.Where(reply => reply.Comment.Id == id).Include(e => e.Owner)
            .ThenInclude(e => e.Region).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        if (currentUserId != null)
            result = result.Include(e => e.Likes.Where(like => like.OwnerId == currentUserId).Take(1));
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<bool> CommentExistsAsync(Guid id)
    {
        return await context.Comments.AnyAsync(like => like.Id == id);
    }

    public async Task<bool> CreateCommentAsync(Comment comment)
    {
        await context.Comments.AddAsync(comment);
        return await SaveAsync();
    }

    public async Task<bool> UpdateCommentAsync(Comment comment)
    {
        context.Comments.Update(comment);
        return await SaveAsync();
    }

    public async Task<bool> RemoveCommentAsync(Guid id)
    {
        context.Comments.Remove((await context.Comments.FirstOrDefaultAsync(comment => comment.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountCommentsAsync(Expression<Func<Comment, bool>>? predicate = null)
    {
        var result = context.Comments.AsQueryable();

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }

    public async Task<int> CountCommentRepliesAsync(Guid id, Expression<Func<Reply, bool>>? predicate = null)
    {
        var comment = (await context.Comments.FirstOrDefaultAsync(comment => comment.Id == id))!;
        var result = context.Replies.Where(record => record.Comment.Id == comment.Id);

        if (predicate != null) result = result.Where(predicate);

        return await result.CountAsync();
    }
}