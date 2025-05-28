using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class FileMigrationService(IServiceProvider serviceProvider, int position = 0) : IHostedService
{
    private IChartRepository _chartRepository = null!;
    private IChartSubmissionRepository _chartSubmissionRepository = null!;
    private IFileStorageService _fileStorageService = null!;
    private ILogger<FileMigrationService> _logger = null!;
    private ISongRepository _songRepository = null!;
    private ISongSubmissionRepository _songSubmissionRepository = null!;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

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
        var songs = await _songRepository.GetSongsAsync(position: position);
        var i = 0;
        foreach (var song in songs)
        {
            _logger.LogInformation(LogEvents.FileMigration,
                "Migrating files for Song #{Id} {Current} / {Total}",
                song.Id, ++i, songs.Count);

            if (song.File != null)
                (song.File, song.FileChecksum) = await MigrateFileAsync<Song>(song.File, song.Title, cancellationToken);

            song.Illustration = (await MigrateFileAsync<Song>(song.Illustration, song.Title, cancellationToken)).Item1;
            foreach (var submission in await _songSubmissionRepository.GetSongSubmissionsAsync(predicate: e =>
                         e.RepresentationId == song.Id))
            {
                submission.File = song.File;
                submission.FileChecksum = song.FileChecksum;
                submission.Illustration = song.Illustration;
                await _songSubmissionRepository.UpdateSongSubmissionAsync(submission);
            }

            var charts = await _chartRepository.GetChartsAsync(predicate: e => e.SongId == song.Id);
            var j = 0;
            foreach (var chart in charts)
            {
                _logger.LogInformation(LogEvents.FileMigration,
                    "Migrating files for Chart #{Id} {Current} / {Total}",
                    chart.Id, ++j, charts.Count);
                if (chart.File == null) continue;
                (chart.File, chart.FileChecksum) =
                    await MigrateFileAsync<Chart>(chart.File, chart.Title ?? song.Title, cancellationToken);
                foreach (var submission in await _chartSubmissionRepository.GetChartSubmissionsAsync(predicate: e =>
                             e.RepresentationId == chart.Id))
                {
                    submission.File = chart.File;
                    submission.FileChecksum = chart.FileChecksum;
                    await _chartSubmissionRepository.UpdateChartSubmissionAsync(submission);
                }
            }

            await _chartRepository.UpdateChartsAsync(charts);
        }

        await _songRepository.UpdateSongsAsync(songs);
    }

    private async Task<(string, string)> MigrateFileAsync<T>(string url, string fileName,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(20);
        var content = await client.GetByteArrayAsync(url, cancellationToken);
        return await _fileStorageService.Upload<T>(fileName, new MemoryStream(content), url.Split('.')[^1]);
    }
}