using System.Linq.Expressions;
using PhiZoneApi.Models;

namespace PhiZoneApi.Interfaces;

public interface IParticipationRepository
{
    Task<ICollection<Participation>> GetEventTeamsAsync(int participantId, List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Participation, bool>>? predicate = null);

    Task<ICollection<Participation>> GetParticipantsAsync(Guid eventTeamId, List<string> order, List<bool> desc, int position,
        int take, Expression<Func<Participation, bool>>? predicate = null);

    Task<ICollection<Participation>> GetParticipationsAsync(List<string> order, List<bool> desc, int position, int take,
        Expression<Func<Participation, bool>>? predicate = null);

    Task<Participation> GetParticipationAsync(Guid eventTeamId, int participantId);

    Task<bool> CreateParticipationAsync(Participation participation);

    Task<bool> UpdateParticipationAsync(Participation participation);

    Task<bool> RemoveParticipationAsync(Guid eventTeamId, int participantId);

    Task<bool> SaveAsync();

    Task<int> CountParticipationsAsync(Expression<Func<Participation, bool>>? predicate = null);

    Task<bool> ParticipationExistsAsync(Guid eventTeamId, int participantId);

    Task<int> CountEventTeamsAsync(int participantId, Expression<Func<Participation, bool>>? predicate = null);

    Task<int> CountParticipantsAsync(Guid eventTeamId, Expression<Func<Participation, bool>>? predicate = null);
}