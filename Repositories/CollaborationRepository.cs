using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class CollaborationRepository(ApplicationDbContext context) : ICollaborationRepository
{
    public async Task<ICollection<Collaboration>> GetCollaborationsAsync(List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Collaboration, bool>>? predicate = null)
    {
        var result = context.Collaborations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Collaboration> GetCollaborationAsync(Guid id)
    {
        return (await context.Collaborations.FirstOrDefaultAsync(collaboration => collaboration.Id == id))!;
    }

    public async Task<Collaboration> GetCollaborationAsync(Guid submissionId, int inviteeId)
    {
        return (await context.Collaborations.FirstOrDefaultAsync(collaboration =>
            collaboration.SubmissionId == submissionId && collaboration.InviteeId == inviteeId))!;
    }

    public async Task<bool> CreateCollaborationAsync(Collaboration collaboration)
    {
        await context.Collaborations.AddAsync(collaboration);
        return await SaveAsync();
    }

    public async Task<bool> UpdateCollaborationAsync(Collaboration collaboration)
    {
        context.Collaborations.Update(collaboration);
        return await SaveAsync();
    }

    public async Task<bool> RemoveCollaborationAsync(Guid id)
    {
        context.Collaborations.Remove(
            (await context.Collaborations.FirstOrDefaultAsync(collaboration => collaboration.Id == id))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountCollaborationsAsync(Expression<Func<Collaboration, bool>>? predicate = null)
    {
        if (predicate != null) return await context.Collaborations.Where(predicate).CountAsync();
        return await context.Collaborations.CountAsync();
    }

    public async Task<bool> CollaborationExistsAsync(Guid id)
    {
        return await context.Collaborations.AnyAsync(collaboration => collaboration.Id == id);
    }

    public async Task<bool> CollaborationExistsAsync(Guid submissionId, int inviteeId)
    {
        return await context.Collaborations.AnyAsync(collaboration =>
            collaboration.SubmissionId == submissionId && collaboration.InviteeId == inviteeId);
    }
}