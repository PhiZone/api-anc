using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class ChartMigrationService(IServiceProvider serviceProvider) : IHostedService
{
    private IChartRepository _chartRepository = null!;
    private ILogger<ChartMigrationService> _logger = null!;
    private IRecordRepository _recordRepository = null!;
    private IRecordService _recordService = null!;
    private IVoteService _voteService = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<ChartMigrationService>>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _recordRepository = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        _voteService = scope.ServiceProvider.GetRequiredService<IVoteService>();
        _recordService = scope.ServiceProvider.GetRequiredService<IRecordService>();

        _logger.LogInformation(LogEvents.ChartMigration, "[{Now}] Chart migration started",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        try
        {
            await MigrateChartsAsync();
            _logger.LogInformation(LogEvents.ChartMigration, "[{Now}] Chart migration finished",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.ChartMigration, ex, "[{Now}] Chart migration failed",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateChartsAsync()
    {
        var charts =
            await _chartRepository.GetChartsAsync();
        foreach (var chart in charts)
        {
            _logger.LogInformation(LogEvents.ChartMigration, "[{Now}] Migrating Chart #{Id}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), chart.Id);
            var records =
                await _recordRepository.GetRecordsAsync(predicate: e => e.ChartId == chart.Id);
            foreach (var record in records)
            {
                var rksFactor = _recordService.CalculateRksFactor(record.PerfectJudgment, record.GoodJudgment);
                record.Rks = _recordService.CalculateRks(record.Perfect, record.GoodEarly + record.GoodLate, record.Bad,
                    record.Miss,
                    chart.Difficulty, record.StdDeviation) * rksFactor;
            }

            await _recordRepository.UpdateRecordsAsync(records);
            chart.PlayCount = records.Count;
            await _voteService.UpdateChartAsync(chart);
        }

        // await _chartRepository.UpdateChartsAsync(charts);
    }
}