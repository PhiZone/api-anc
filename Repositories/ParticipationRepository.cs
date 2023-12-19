using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using PhiZoneApi.Utils;

namespace PhiZoneApi.Repositories;

public class ParticipationRepository(ApplicationDbContext context) : IParticipationRepository
{
    public async Task<ICollection<Participation>> GetEventTeamsAsync(int participantId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Participation, bool>>? predicate = null)
    {
        var result = context.Participations.Where(participation => participation.ParticipantId == participantId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Participation>> GetParticipantsAsync(Guid eventTeamId, List<string> order, List<bool> desc,
        int position,
        int take, Expression<Func<Participation, bool>>? predicate = null)
    {
        var result = context.Participations.Where(participation => participation.EventTeamId == eventTeamId).OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<ICollection<Participation>> GetParticipationsAsync(List<string> order, List<bool> desc, int position,
        int take,
        Expression<Func<Participation, bool>>? predicate = null)
    {
        var result = context.Participations.OrderBy(order, desc);
        if (predicate != null) result = result.Where(predicate);
        result = result.Skip(position);
        return take >= 0 ? await result.Take(take).ToListAsync() : await result.ToListAsync();
    }

    public async Task<Participation> GetParticipationAsync(Guid eventTeamId, int participantId)
    {
        return (await context.Participations.FirstOrDefaultAsync(participation =>
            participation.EventTeamId == eventTeamId && participation.ParticipantId == participantId))!;
    }

    public async Task<bool> CreateParticipationAsync(Participation participation)
    {
        await context.Participations.AddAsync(participation);
        return await SaveAsync();
    }

    public async Task<bool> UpdateParticipationAsync(Participation participation)
    {
        context.Participations.Update(participation);
        return await SaveAsync();
    }

    public async Task<bool> RemoveParticipationAsync(Guid eventTeamId, int participantId)
    {
        context.Participations.Remove((await context.Participations.FirstOrDefaultAsync(participation =>
            participation.EventTeamId == eventTeamId && participation.ParticipantId == participantId))!);
        return await SaveAsync();
    }

    public async Task<bool> SaveAsync()
    {
        var saved = await context.SaveChangesAsync();
        return saved > 0;
    }

    public async Task<int> CountParticipationsAsync(Expression<Func<Participation, bool>>? predicate = null)
    {
        if (predicate != null) return await context.Participations.Where(predicate).CountAsync();
        return await context.Participations.CountAsync();
    }

    public async Task<bool> ParticipationExistsAsync(Guid eventTeamId, int participantId)
    {
        return await context.Participations.AnyAsync(participation =>
            participation.EventTeamId == eventTeamId && participation.ParticipantId == participantId);
    }

    public async Task<int> CountEventTeamsAsync(int participantId, Expression<Func<Participation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.Participations.Where(participation => participation.ParticipantId == participantId)
                .Where(predicate)
                .CountAsync();

        return await context.Participations.Where(participation => participation.ParticipantId == participantId).CountAsync();
    }

    public async Task<int> CountParticipantsAsync(Guid eventTeamId, Expression<Func<Participation, bool>>? predicate = null)
    {
        if (predicate != null)
            return await context.Participations.Where(participation => participation.EventTeamId == eventTeamId)
                .Where(predicate)
                .CountAsync();

        return await context.Participations.Where(participation => participation.EventTeamId == eventTeamId).CountAsync();
    }
}