using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class LeaderboardService
{
    public LeaderboardService()
    {
        
    }

    public class Leaderboard<T>
    {
        private readonly SortedSet<T> _set;

        public Leaderboard(IComparer<T> comparer)
        {
            _set = new SortedSet<T>(comparer);
        }
    }
}