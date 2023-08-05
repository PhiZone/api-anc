using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IChartRepository _chartRepository;
    private readonly IChartSubmissionRepository _chartSubmissionRepository;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;
    private readonly ISongRepository _songRepository;
    private readonly IUserService _userService;

    public SubmissionService(ISongRepository songRepository, INotificationService notificationService,
        IResourceService resourceService, IUserService userService, IChartRepository chartRepository,
        IChartSubmissionRepository chartSubmissionRepository)
    {
        _songRepository = songRepository;
        _notificationService = notificationService;
        _resourceService = resourceService;
        _userService = userService;
        _chartRepository = chartRepository;
        _chartSubmissionRepository = chartSubmissionRepository;
    }

    public async Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal)
    {
        Song song;
        if (songSubmission.RepresentationId == null)
        {
            song = new Song
            {
                Title = songSubmission.Title,
                EditionType = songSubmission.EditionType,
                Edition = songSubmission.Edition,
                File = songSubmission.File,
                FileChecksum = songSubmission.FileChecksum,
                AuthorName = songSubmission.AuthorName,
                Illustration = songSubmission.Illustration,
                Illustrator = songSubmission.Illustrator,
                Description = songSubmission.Description,
                Accessibility = songSubmission.Accessibility,
                IsHidden = false,
                IsLocked = false,
                Lyrics = songSubmission.Lyrics,
                Bpm = songSubmission.Bpm,
                MinBpm = songSubmission.MinBpm,
                MaxBpm = songSubmission.MaxBpm,
                Offset = songSubmission.Offset,
                IsOriginal = isOriginal,
                Duration = songSubmission.Duration,
                PreviewStart = songSubmission.PreviewStart,
                PreviewEnd = songSubmission.PreviewEnd,
                OwnerId = songSubmission.OwnerId,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await _songRepository.CreateSongAsync(song);
        }
        else
        {
            song = await _songRepository.GetSongAsync(songSubmission.RepresentationId.Value);

            song.Title = songSubmission.Title;
            song.EditionType = songSubmission.EditionType;
            song.Edition = songSubmission.Edition;
            song.File = songSubmission.File;
            song.FileChecksum = songSubmission.FileChecksum;
            song.AuthorName = songSubmission.AuthorName;
            song.Illustration = songSubmission.Illustration;
            song.Illustrator = songSubmission.Illustrator;
            song.Description = songSubmission.Description;
            song.Accessibility = songSubmission.Accessibility;
            song.IsHidden = false;
            song.IsLocked = false;
            song.Lyrics = songSubmission.Lyrics;
            song.Bpm = songSubmission.Bpm;
            song.MinBpm = songSubmission.MinBpm;
            song.MaxBpm = songSubmission.MaxBpm;
            song.Offset = songSubmission.Offset;
            song.IsOriginal = isOriginal;
            song.Duration = songSubmission.Duration;
            song.PreviewStart = songSubmission.PreviewStart;
            song.PreviewEnd = songSubmission.PreviewEnd;
            song.OwnerId = songSubmission.OwnerId;
            song.DateUpdated = DateTimeOffset.UtcNow;

            await _songRepository.UpdateSongAsync(song);
        }

        await _notificationService.Notify(songSubmission.Owner, await _userService.GetOfficial(),
            NotificationType.System, "song-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Song",
                    _resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                }
            });

        foreach (var chartSubmission in await _chartSubmissionRepository.GetChartSubmissionsAsync("DateCreated", false,
                     0, -1, predicate:
                     e => e.Status == RequestStatus.Approved))
            await ApproveChart(chartSubmission);
        return song;
    }

    public async Task RejectSong(SongSubmission songSubmission)
    {
        await _notificationService.Notify(songSubmission.Owner, await _userService.GetOfficial(),
            NotificationType.System, "song-submission-rejection",
            new Dictionary<string, string>
            {
                {
                    "Song",
                    _resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                },
                { "Reason", songSubmission.Message! }
            });
    }

    public async Task ApproveChart(ChartSubmission chartSubmission)
    {
        if (chartSubmission.SongId == null) return;

        var chart = new Chart
        {
            Title = chartSubmission.Title,
            LevelType = chartSubmission.LevelType,
            Level = chartSubmission.Level,
            Difficulty = chartSubmission.Difficulty,
            Format = chartSubmission.Format,
            File = chartSubmission.File,
            FileChecksum = chartSubmission.FileChecksum,
            AuthorName = chartSubmission.AuthorName,
            Illustration = chartSubmission.Illustration,
            Illustrator = chartSubmission.Illustrator,
            Description = chartSubmission.Description,
            Accessibility = chartSubmission.Accessibility,
            IsRanked = chartSubmission.IsRanked,
            IsHidden = false,
            IsLocked = false,
            NoteCount = chartSubmission.NoteCount,
            SongId = chartSubmission.SongId.Value,
            OwnerId = chartSubmission.OwnerId,
            DateCreated = DateTimeOffset.UtcNow,
            DateUpdated = DateTimeOffset.UtcNow
        };
        await _chartRepository.CreateChartAsync(chart);

        await _notificationService.Notify(chartSubmission.Owner, await _userService.GetOfficial(),
            NotificationType.System, "chart-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        chartSubmission.GetDisplay())
                }
            });

        chartSubmission.RepresentationId = chart.Id;
        chartSubmission.DateUpdated = DateTimeOffset.UtcNow;
        await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission);
    }

    public async Task RejectChart(ChartSubmission chartSubmission)
    {
        await _notificationService.Notify(chartSubmission.Owner, await _userService.GetOfficial(),
            NotificationType.System, "chart-submission-rejection",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        chartSubmission.GetDisplay())
                }
            });
    }
}