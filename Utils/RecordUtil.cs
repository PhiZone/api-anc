using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Utils;

public static class RecordUtil
{
    public static int CalculateScore(int perfect, int good, int bad, int miss, int maxCombo)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0)
        {
            return 0;
        }

        return (int)Math.Round((double)(900000 * perfect + 58500 * good + 100000 * maxCombo) / totalCount, 0);
    }

    public static double CalculateAccuracy(int perfect, int good, int bad, int miss)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0)
        {
            return 0;
        }

        return (perfect + 0.65 * good) / totalCount;
    }

    public static double CalculateRks(int perfect, int good, int bad, int miss, double difficulty, double stdDeviation)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0)
        {
            return 0;
        }

        var accuracy = (double)(100 * perfect + 65 * good) / totalCount;
        if (accuracy < 70)
        {
            return 0;
        }

        accuracy -= 55;
        return accuracy * accuracy * difficulty / 2025 + 0.032 - stdDeviation / 2;
    }

    public static double CalculateRksFactor(int perfectJudgment, int goodJudgment)
    {
        var x = 0.8 * perfectJudgment + 0.225 * goodJudgment;
        switch (x)
        {
            case > 150:
                return 0;
            case > 100:
                return x * x / 7500 - 4 * x / 75 + 5;
            default:
                x -= 100;
                return -x * x * x / 4000000 + 1;
        }
    }

    public static async Task<List<Record>> GetBest19(int userId, IRecordRepository repository)
    {
        var result = new List<Record>();
        var charts = new List<Guid>();
        for (var position = 0; result.Count < 19; position += 30)
        {
            var records = await repository.GetRecordsAsync("Rks", true, position, 30,
                record => record.OwnerId == userId && record.Chart.IsRanked);
            foreach (var record in records)
            {
                if (charts.Contains(record.ChartId))
                {
                    continue;
                }
                result.Add(record);
                charts.Add(record.ChartId);
                if (result.Count >= 19)
                {
                    break;
                }
            }
        }

        return result;
    }
}