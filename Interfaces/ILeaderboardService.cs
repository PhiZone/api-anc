using PhiZoneApi.Models;
using PhiZoneApi.Services;

namespace PhiZoneApi.Interfaces;

public interface ILeaderboardService
{
    LeaderboardService.Leaderboard<Record> ObtainChartLeaderboard(Guid chart);

    LeaderboardService.Leaderboard<EventTeam> ObtainEventDivisionLeaderboard(Guid eventDivision);

    bool Add(Record record);

    bool Remove(Record record);

    bool Add(EventTeam eventTeam);

    bool Remove(EventTeam eventTeam);
}