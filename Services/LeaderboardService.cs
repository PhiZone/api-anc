using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Data;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class LeaderboardService : ILeaderboardService
{
    public class Leaderboard<T> where T : IComparable<T>
    {
        private readonly SortedSet<T> _set = [];

        public IEnumerable<T> Range(int skip, int take)
        {
            return _set.Skip(skip).Take(take);
        }

        public int? GetRank(T? item)
        {
            if (item == null) return null;
            var index = _set.ToImmutableSortedSet().IndexOf(item);
            return index >= 0 ? index + 1 : null;
        }

        public bool Add(T item)
        {
            return _set.Add(item);
        }

        public bool Remove(T? item)
        {
            return item == null || _set.Remove(item);
        }

        public T? FirstOrDefault(Func<T, bool> predicate)
        {
            return _set.FirstOrDefault(predicate);
        }

        public SortedSet<T> GetSet()
        {
            return _set;
        }
    }

    private readonly Dictionary<Guid, Leaderboard<Record>> _chartLeaderboards = new();
    private readonly Dictionary<Guid, Leaderboard<EventTeam>> _eventDivisionLeaderboards = new();

    public async Task Initialize(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        foreach (var chart in await context.Charts.ToListAsync(cancellationToken))
        {
            var leaderboard = ObtainChartLeaderboard(chart.Id);
            foreach (var record in context.Records.Include(e => e.Owner).ThenInclude(e => e.Region).Where(e => e.ChartId == chart.Id)
                         .GroupBy(e => e.OwnerId)
                         .Select(g => g.OrderByDescending(e => e.Rks).ThenBy(e => e.DateCreated).First()))
            {
                leaderboard.Add(record);
            }
        }

        foreach (var eventDivision in await context.EventDivisions.ToListAsync(cancellationToken))
        {
            var leaderboard = ObtainEventDivisionLeaderboard(eventDivision.Id);
            foreach (var eventTeam in context.EventTeams.Where(e => e.DivisionId == eventDivision.Id))
            {
                leaderboard.Add(eventTeam);
            }
        }
    }

    public Leaderboard<Record> ObtainChartLeaderboard(Guid chart)
    {
        if (_chartLeaderboards.TryGetValue(chart, out var leaderboard))
        {
            return leaderboard;
        }

        leaderboard = new Leaderboard<Record>();
        _chartLeaderboards.Add(chart, leaderboard);
        return leaderboard;
    }

    public Leaderboard<EventTeam> ObtainEventDivisionLeaderboard(Guid eventDivision)
    {
        if (_eventDivisionLeaderboards.TryGetValue(eventDivision, out var leaderboard))
        {
            return leaderboard;
        }

        leaderboard = new Leaderboard<EventTeam>();
        _eventDivisionLeaderboards.Add(eventDivision, leaderboard);
        return leaderboard;
    }

    public bool Add(Record record)
    {
        var leaderboard = ObtainChartLeaderboard(record.ChartId);
        var existingItem = leaderboard.FirstOrDefault(e => e.OwnerId == record.OwnerId);
        if (existingItem == null)
        {
            return leaderboard.Add(record);
        }

        if (existingItem.CompareTo(record) <= 0)
        {
            return false;
        }

        return leaderboard.Remove(existingItem) && leaderboard.Add(record);
    }

    public bool Remove(Record record) => ObtainChartLeaderboard(record.ChartId).Remove(record);

    public bool Add(EventTeam eventTeam)
    {
        var leaderboard = ObtainEventDivisionLeaderboard(eventTeam.DivisionId);
        var existingItem = leaderboard.FirstOrDefault(e => e.Id == eventTeam.Id);
        if (existingItem == null)
        {
            return leaderboard.Add(eventTeam);
        }

        if (existingItem.CompareTo(eventTeam) <= 0)
        {
            return false;
        }

        return leaderboard.Remove(existingItem) && leaderboard.Add(eventTeam);
    }

    public bool Remove(EventTeam eventTeam) => ObtainEventDivisionLeaderboard(eventTeam.DivisionId).Remove(eventTeam);
}