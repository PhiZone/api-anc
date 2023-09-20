using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class RecordService : IRecordService
{
    private readonly IRecordRepository _recordRepository;

    public RecordService(IRecordRepository recordRepository)
    {
        _recordRepository = recordRepository;
    }

    public int CalculateScore(int perfect, int good, int bad, int miss, int maxCombo)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return 0;

        return (int)Math.Round((9e5 * perfect + 585e3 * good + 1e5 * maxCombo) / totalCount, 0);
    }

    public double CalculateAccuracy(int perfect, int good, int bad, int miss)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return 0;

        return (perfect + 0.65 * good) / totalCount;
    }

    public double CalculateRks(int perfect, int good, int bad, int miss, double difficulty, double stdDeviation)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return 0;

        var accuracy = (double)(100 * perfect + 65 * good) / totalCount;
        if (accuracy < 70) return 0;

        accuracy -= 55;
        return accuracy * accuracy * difficulty / 2025 + 2e-2 - stdDeviation / 2e3;
    }

    public double CalculateRksFactor(int perfectJudgment, int goodJudgment)
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
                return -x * x * x / 4e6 + 1;
        }
    }

    public async Task<List<Record>> GetBest19(int userId)
    {
        var result = new List<Record>();
        var charts = new List<Guid>();
        for (var position = 0; result.Count < 19; position += 30)
        {
            var records = await _recordRepository.GetRecordsAsync(new List<string> { "Rks" }, new List<bool> { true },
                position, 30,
                record => record.OwnerId == userId && record.Chart.IsRanked);
            if (records.Count == 0) break;
            foreach (var record in records)
            {
                if (charts.Contains(record.ChartId)) continue;
                result.Add(record);
                charts.Add(record.ChartId);
                if (result.Count >= 19) break;
            }
        }

        return result;
    }
}