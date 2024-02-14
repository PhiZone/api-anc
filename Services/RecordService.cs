using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class RecordService : IRecordService
{
    public int CalculateScore(int perfect, int good, int bad, int miss, int maxCombo)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return 1_000_000;

        return (int)Math.Round((9e5 * perfect + 585e3 * good + 1e5 * maxCombo) / totalCount, 0);
    }

    public double CalculateAccuracy(int perfect, int good, int bad, int miss)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return 1;

        return (perfect + 0.65 * good) / totalCount;
    }

    public double CalculateRks(int perfect, int good, int bad, int miss, double difficulty, double stdDeviation)
    {
        var totalCount = perfect + good + bad + miss;
        if (totalCount == 0) return difficulty;

        var accuracy = (double)(100 * perfect + 65 * good) / totalCount;
        if (accuracy < 70) return 0;

        accuracy -= 55;
        return accuracy * accuracy * difficulty / 2025 + (stdDeviation < 40
            ? 1.25e-5 * (stdDeviation - 40) * (stdDeviation - 40)
            : 2e-2 - stdDeviation / 2e3);
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
}