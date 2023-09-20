using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class SubmissionService : ISubmissionService
{
    private readonly IAuthorshipRepository _authorshipRepository;
    private readonly IChartRepository _chartRepository;
    private readonly IChartSubmissionRepository _chartSubmissionRepository;
    private readonly ICollaborationRepository _collaborationRepository;
    private readonly INotificationService _notificationService;
    private readonly IResourceService _resourceService;
    private readonly ISongRepository _songRepository;
    private readonly UserManager<User> _userManager;
    private readonly IUserRelationRepository _userRelationRepository;

    public SubmissionService(ISongRepository songRepository, INotificationService notificationService,
        IResourceService resourceService, IChartRepository chartRepository,
        IChartSubmissionRepository chartSubmissionRepository, ICollaborationRepository collaborationRepository,
        IAuthorshipRepository authorshipRepository, UserManager<User> userManager,
        IUserRelationRepository userRelationRepository)
    {
        _songRepository = songRepository;
        _notificationService = notificationService;
        _resourceService = resourceService;
        _chartRepository = chartRepository;
        _chartSubmissionRepository = chartSubmissionRepository;
        _collaborationRepository = collaborationRepository;
        _authorshipRepository = authorshipRepository;
        _userManager = userManager;
        _userRelationRepository = userRelationRepository;
    }

    public async Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal, bool isHidden)
    {
        Song song;
        var owner = (await _userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!;
        var description = songSubmission.Description;
        List<User>? mentions = null;

        if (description != null)
        {
            var result = await _resourceService.ParseUserContent(description);
            description = result.Item1;
            mentions = result.Item2;
        }

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
                Description = description,
                Accessibility = songSubmission.Accessibility,
                IsHidden = isHidden,
                IsLocked = false,
                Lyrics = songSubmission.Lyrics,
                Bpm = songSubmission.Bpm,
                MinBpm = songSubmission.MinBpm,
                MaxBpm = songSubmission.MaxBpm,
                Offset = songSubmission.Offset,
                License = songSubmission.License,
                IsOriginal = isOriginal,
                Duration = songSubmission.Duration,
                PreviewStart = songSubmission.PreviewStart,
                PreviewEnd = songSubmission.PreviewEnd,
                OwnerId = songSubmission.OwnerId,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await _songRepository.CreateSongAsync(song);

            if (!isHidden)
                foreach (var relation in await _userRelationRepository.GetRelationsAsync(
                             new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                             e => e.FolloweeId == songSubmission.OwnerId && e.Type != UserRelationType.Blacklisted))
                    await _notificationService.Notify(
                        (await _userManager.FindByIdAsync(relation.FollowerId.ToString()))!,
                        (await _userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!,
                        NotificationType.Updates, "song-follower-update",
                        new Dictionary<string, string>
                        {
                            {
                                "User",
                                _resourceService.GetRichText<User>(songSubmission.OwnerId.ToString(), owner.UserName!)
                            },
                            { "Song", _resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) }
                        });

            foreach (var chartSubmission in await _chartSubmissionRepository.GetChartSubmissionsAsync(
                         new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                         predicate: e => e.Status == RequestStatus.Approved))
                await ApproveChart(chartSubmission);
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
            song.Description = description;
            song.Accessibility = songSubmission.Accessibility;
            song.IsHidden = isHidden;
            song.Lyrics = songSubmission.Lyrics;
            song.Bpm = songSubmission.Bpm;
            song.MinBpm = songSubmission.MinBpm;
            song.MaxBpm = songSubmission.MaxBpm;
            song.Offset = songSubmission.Offset;
            song.License = songSubmission.License;
            song.IsOriginal = isOriginal;
            song.Duration = songSubmission.Duration;
            song.PreviewStart = songSubmission.PreviewStart;
            song.PreviewEnd = songSubmission.PreviewEnd;
            song.OwnerId = songSubmission.OwnerId;
            song.DateUpdated = DateTimeOffset.UtcNow;

            await _songRepository.UpdateSongAsync(song);
        }

        await _notificationService.Notify(songSubmission.Owner, null, NotificationType.System,
            "song-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Song",
                    _resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                }
            });

        foreach (var collaboration in await _collaborationRepository.GetCollaborationsAsync(
                     new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                     e => e.SubmissionId == songSubmission.Id && e.Status == RequestStatus.Approved))
        {
            if (await _authorshipRepository.AuthorshipExistsAsync(song.Id, collaboration.InviteeId)) continue;
            var authorship = new Authorship
            {
                ResourceId = song.Id,
                AuthorId = collaboration.InviteeId,
                Position = collaboration.Position,
                DateCreated = DateTimeOffset.UtcNow
            };
            await _authorshipRepository.CreateAuthorshipAsync(authorship);
        }

        if (description != null && mentions != null)
            await _notificationService.NotifyMentions(mentions, owner,
                _resourceService.GetRichText<Song>(song.Id.ToString(), songSubmission.Description!));

        return song;
    }

    public async Task RejectSong(SongSubmission songSubmission)
    {
        await _notificationService.Notify((await _userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!, null,
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

        Chart chart;
        var owner = (await _userManager.FindByIdAsync(chartSubmission.OwnerId.ToString()))!;
        var description = chartSubmission.Description;
        List<User>? mentions = null;

        if (description != null)
        {
            var result = await _resourceService.ParseUserContent(description);
            description = result.Item1;
            mentions = result.Item2;
        }

        if (chartSubmission.RepresentationId == null)
        {
            chart = new Chart
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
            chartSubmission.RepresentationId = chart.Id;
            await _chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission);

            if (!(await _songRepository.GetSongAsync(chartSubmission.SongId.Value)).IsHidden)
                foreach (var relation in await _userRelationRepository.GetRelationsAsync(
                             new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                             e => e.FolloweeId == chartSubmission.OwnerId && e.Type != UserRelationType.Blacklisted))
                    await _notificationService.Notify(
                        (await _userManager.FindByIdAsync(relation.FollowerId.ToString()))!,
                        (await _userManager.FindByIdAsync(chartSubmission.OwnerId.ToString()))!,
                        NotificationType.Updates, "chart-follower-update",
                        new Dictionary<string, string>
                        {
                            {
                                "User",
                                _resourceService.GetRichText<User>(chartSubmission.OwnerId.ToString(), owner.UserName!)
                            },
                            {
                                "Chart",
                                _resourceService.GetRichText<Chart>(chart.Id.ToString(),
                                    await _resourceService.GetDisplayName(chart))
                            }
                        });
        }
        else
        {
            chart = await _chartRepository.GetChartAsync(chartSubmission.RepresentationId.Value);
            chart.Title = chartSubmission.Title;
            chart.LevelType = chartSubmission.LevelType;
            chart.Level = chartSubmission.Level;
            chart.Difficulty = chartSubmission.Difficulty;
            chart.Format = chartSubmission.Format;
            chart.File = chartSubmission.File;
            chart.FileChecksum = chartSubmission.FileChecksum;
            chart.AuthorName = chartSubmission.AuthorName;
            chart.Illustration = chartSubmission.Illustration;
            chart.Illustrator = chartSubmission.Illustrator;
            chart.Description = chartSubmission.Description;
            chart.Accessibility = chartSubmission.Accessibility;
            chart.IsRanked = chartSubmission.IsRanked;
            chart.IsHidden = false;
            chart.IsLocked = false;
            chart.NoteCount = chartSubmission.NoteCount;
            chart.SongId = chartSubmission.SongId.Value;
            chart.OwnerId = chartSubmission.OwnerId;
            chart.DateUpdated = DateTimeOffset.UtcNow;
            await _chartRepository.UpdateChartAsync(chart);
        }

        await _notificationService.Notify(chartSubmission.Owner, null, NotificationType.System,
            "chart-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await _resourceService.GetDisplayName(chartSubmission))
                }
            });

        foreach (var collaboration in await _collaborationRepository.GetCollaborationsAsync(
                     new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                     e => e.SubmissionId == chartSubmission.Id && e.Status == RequestStatus.Approved))
        {
            if (await _authorshipRepository.AuthorshipExistsAsync(chart.Id, collaboration.InviteeId)) continue;
            var authorship = new Authorship
            {
                ResourceId = chart.Id,
                AuthorId = collaboration.InviteeId,
                Position = collaboration.Position,
                DateCreated = DateTimeOffset.UtcNow
            };
            await _authorshipRepository.CreateAuthorshipAsync(authorship);
        }

        if (description != null && mentions != null)
            await _notificationService.NotifyMentions(mentions, owner,
                _resourceService.GetRichText<Chart>(chart.Id.ToString(), chartSubmission.Description!));
    }

    public async Task RejectChart(ChartSubmission chartSubmission)
    {
        await _notificationService.Notify(chartSubmission.Owner, null, NotificationType.System,
            "chart-submission-rejection",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    _resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await _resourceService.GetDisplayName(chartSubmission))
                }
            });
    }
}