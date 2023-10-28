namespace PhiZoneApi.Services;

public class LeaderboardService
{
    public class Leaderboard<T>
    {
        private readonly SortedSet<T> _set;

        public Leaderboard(IComparer<T> comparer)
        {
            _set = new SortedSet<T>(comparer);
        }
    }
}