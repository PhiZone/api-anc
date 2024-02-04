namespace PhiZoneApi.Interfaces;

public interface IRecordService
{
    int CalculateScore(int perfect, int good, int bad, int miss, int maxCombo);

    double CalculateAccuracy(int perfect, int good, int bad, int miss);

    double CalculateRks(int perfect, int good, int bad, int miss, double difficulty, double stdDeviation);

    double CalculateRksFactor(int perfectJudgment, int goodJudgment);
}