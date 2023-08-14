using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class ChartMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private IChartRepository _chartRepository = null!;
    private ILogger<DataMigrationService> _logger = null!;
    private IRecordRepository _recordRepository = null!;
    private IRecordService _recordService = null!;

    public ChartMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<DataMigrationService>>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _recordRepository = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
        _recordService = scope.ServiceProvider.GetRequiredService<IRecordService>();

        _logger.LogInformation(LogEvents.DataMigration, "Chart migration started");
        try
        {
            await MigrateChartAsync();
            _logger.LogInformation(LogEvents.DataMigration, "Chart migration finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.DataMigration, ex, "Chart migration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateChartAsync()
    {
        var charts = await _chartRepository.GetChartsAsync("DateCreated", false, 0, -1);
        foreach (var chart in charts)
        {
            var records = await _recordRepository.GetRecordsAsync("DateCreated", false, 0, -1);
            foreach (var record in records)
            {
                var rksFactor = _recordService.CalculateRksFactor(record.PerfectJudgment, record.GoodJudgment);
                record.Rks = _recordService.CalculateRks(record.Perfect, record.GoodEarly + record.GoodLate, record.Bad,
                    record.Miss,
                    chart.Difficulty, record.StdDeviation) * rksFactor;
            }

            await _recordRepository.UpdateRecordsAsync(records);
            chart.PlayCount = records.Count;
        }

        await _chartRepository.UpdateChartsAsync(charts);
    }
}