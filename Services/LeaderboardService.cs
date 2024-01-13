
namespace PhiZoneApi.Services;

public class LeaderboardService
{
    public class Leaderboard<T> where T : IComparable<T>
    {
        private readonly SortedSet<T> _set = new();

        public IEnumerable<T> Range(int skip, int take)
        {
            return _set.Skip(skip).Take(take);
        }

        public SortedSet<T> GetSet()
        {
            return _set;
        }
    }
}