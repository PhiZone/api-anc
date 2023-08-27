using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class FileMigrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ILogger<FileMigrationService> _logger = null!;
    private ISongRepository _songRepository = null!;
    private IChartRepository _chartRepository = null!;
    private ISongSubmissionRepository _songSubmissionRepository = null!;
    private IChartSubmissionRepository _chartSubmissionRepository = null!;
    private IFileStorageService _fileStorageService = null!;

    public FileMigrationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();

        _logger = scope.ServiceProvider.GetRequiredService<ILogger<FileMigrationService>>();
        _fileStorageService = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
        _songRepository = scope.ServiceProvider.GetRequiredService<ISongRepository>();
        _chartRepository = scope.ServiceProvider.GetRequiredService<IChartRepository>();
        _songSubmissionRepository = scope.ServiceProvider.GetRequiredService<ISongSubmissionRepository>();
        _chartSubmissionRepository = scope.ServiceProvider.GetRequiredService<IChartSubmissionRepository>();

        _logger.LogInformation(LogEvents.FileMigration, "File migration started");
        try
        {
            await MigrateFilesAsync(cancellationToken);
            _logger.LogInformation(LogEvents.FileMigration, "File migration finished");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogEvents.FileMigration, ex, "File migration failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task MigrateFilesAsync(CancellationToken cancellationToken)
    {
        var songs = await _songRepository.GetSongsAsync("DateCreated", false, 0, -1);
        foreach (var song in songs)
        {
            _logger.LogInformation(LogEvents.FileMigration, "Migrating files for Song #{Id}", song.Id);
            var charts = await _songRepository.GetSongChartsAsync(song.Id, "DateCreated", false, 0, -1);
            foreach (var chart in charts)
            {
                if (chart.File == null) continue;
                _logger.LogInformation(LogEvents.FileMigration, "Migrating files for Chart #{Id}", chart.Id);
                (chart.File, chart.FileChecksum) =
                    await MigrateFileAsync<Chart>(chart.File, chart.Title ?? song.Title, cancellationToken);
                foreach (var submission in await _chartSubmissionRepository.GetChartSubmissionsAsync("DateCreated",
                             false, 0, -1, predicate: e => e.RepresentationId == chart.Id))
                {
                    submission.File = chart.File;
                    submission.FileChecksum = chart.FileChecksum;
                    await _chartSubmissionRepository.UpdateChartSubmissionAsync(submission);
                }
            }

            await _chartRepository.UpdateChartsAsync(charts);
            if (song.File == null) continue;
            (song.File, song.FileChecksum) = await MigrateFileAsync<Song>(song.File, song.Title, cancellationToken);
            song.Illustration = (await MigrateFileAsync<Song>(song.Illustration, song.Title, cancellationToken)).Item1;
            foreach (var submission in await _songSubmissionRepository.GetSongSubmissionsAsync("DateCreated",
                         false, 0, -1, predicate: e => e.RepresentationId == song.Id))
            {
                submission.File = song.File;
                submission.FileChecksum = song.FileChecksum;
                submission.Illustration = song.Illustration;
                await _songSubmissionRepository.UpdateSongSubmissionAsync(submission);
            }
        }

        await _songRepository.UpdateSongsAsync(songs);
    }

    private async Task<(string, string)> MigrateFileAsync<T>(string url, string fileName,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var content = await client.GetByteArrayAsync(url, cancellationToken);
        return await _fileStorageService.Upload<T>(fileName, new MemoryStream(content), url.Split('.')[^1]);
    }
}