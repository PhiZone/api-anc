using Microsoft.AspNetCore.Identity;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;

namespace PhiZoneApi.Services;

public class SubmissionService(ISongRepository songRepository, INotificationService notificationService,
        IResourceService resourceService, IChartRepository chartRepository,
        IChartSubmissionRepository chartSubmissionRepository, ICollaborationRepository collaborationRepository,
        IAuthorshipRepository authorshipRepository, UserManager<User> userManager,
        IUserRelationRepository userRelationRepository)
    : ISubmissionService
{
    public async Task<Song> ApproveSong(SongSubmission songSubmission, bool isOriginal, bool isHidden, bool isLocked)
    {
        Song song;
        var owner = (await userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!;
        var description = songSubmission.Description;
        List<User>? mentions = null;

        if (description != null)
        {
            var result = await resourceService.ParseUserContent(description);
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
                IsLocked = isLocked,
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
            await songRepository.CreateSongAsync(song);

            if (!isHidden)
                foreach (var relation in await userRelationRepository.GetRelationsAsync(
                             new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                             e => e.FolloweeId == songSubmission.OwnerId && e.Type != UserRelationType.Blacklisted))
                    await notificationService.Notify(
                        (await userManager.FindByIdAsync(relation.FollowerId.ToString()))!,
                        (await userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!,
                        NotificationType.Updates, "song-follower-update",
                        new Dictionary<string, string>
                        {
                            {
                                "User",
                                resourceService.GetRichText<User>(songSubmission.OwnerId.ToString(), owner.UserName!)
                            },
                            { "Song", resourceService.GetRichText<Song>(song.Id.ToString(), song.GetDisplay()) }
                        });

            foreach (var chartSubmission in await chartSubmissionRepository.GetChartSubmissionsAsync(
                         new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                         predicate: e => e.Status == RequestStatus.Approved))
                await ApproveChart(chartSubmission, song.Id);
        }
        else
        {
            song = await songRepository.GetSongAsync(songSubmission.RepresentationId.Value);

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
            song.IsLocked = isLocked;
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

            await songRepository.UpdateSongAsync(song);
        }

        await notificationService.Notify(songSubmission.Owner, null, NotificationType.System,
            "song-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Song",
                    resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                }
            });

        foreach (var collaboration in await collaborationRepository.GetCollaborationsAsync(
                     new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                     e => e.SubmissionId == songSubmission.Id && e.Status == RequestStatus.Approved))
        {
            if (await authorshipRepository.AuthorshipExistsAsync(song.Id, collaboration.InviteeId)) continue;
            var authorship = new Authorship
            {
                ResourceId = song.Id,
                AuthorId = collaboration.InviteeId,
                Position = collaboration.Position,
                DateCreated = DateTimeOffset.UtcNow
            };
            await authorshipRepository.CreateAuthorshipAsync(authorship);
        }

        if (description != null && mentions != null)
            await notificationService.NotifyMentions(mentions, owner,
                resourceService.GetRichText<Song>(song.Id.ToString(), songSubmission.Description!));

        return song;
    }

    public async Task RejectSong(SongSubmission songSubmission)
    {
        await notificationService.Notify((await userManager.FindByIdAsync(songSubmission.OwnerId.ToString()))!, null,
            NotificationType.System, "song-submission-rejection",
            new Dictionary<string, string>
            {
                {
                    "Song",
                    resourceService.GetRichText<SongSubmission>(songSubmission.Id.ToString(),
                        songSubmission.GetDisplay())
                },
                { "Reason", songSubmission.Message! }
            });
    }

    public async Task ApproveChart(ChartSubmission chartSubmission, Guid? songId = null)
    {
        if (chartSubmission.SongId == null && songId == null) return;

        Chart chart;
        var owner = (await userManager.FindByIdAsync(chartSubmission.OwnerId.ToString()))!;
        var description = chartSubmission.Description;
        List<User>? mentions = null;

        if (description != null)
        {
            var result = await resourceService.ParseUserContent(description);
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
                SongId = (chartSubmission.SongId ?? songId)!.Value,
                OwnerId = chartSubmission.OwnerId,
                DateCreated = DateTimeOffset.UtcNow,
                DateUpdated = DateTimeOffset.UtcNow
            };
            await chartRepository.CreateChartAsync(chart);
            chartSubmission.RepresentationId = chart.Id;
            await chartSubmissionRepository.UpdateChartSubmissionAsync(chartSubmission);

            if (!(await songRepository.GetSongAsync(chartSubmission.SongId!.Value)).IsHidden)
                foreach (var relation in await userRelationRepository.GetRelationsAsync(
                             new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                             e => e.FolloweeId == chartSubmission.OwnerId && e.Type != UserRelationType.Blacklisted))
                    await notificationService.Notify(
                        (await userManager.FindByIdAsync(relation.FollowerId.ToString()))!,
                        (await userManager.FindByIdAsync(chartSubmission.OwnerId.ToString()))!,
                        NotificationType.Updates, "chart-follower-update",
                        new Dictionary<string, string>
                        {
                            {
                                "User",
                                resourceService.GetRichText<User>(chartSubmission.OwnerId.ToString(), owner.UserName!)
                            },
                            {
                                "Chart",
                                resourceService.GetRichText<Chart>(chart.Id.ToString(),
                                    await resourceService.GetDisplayName(chart))
                            }
                        });
        }
        else
        {
            chart = await chartRepository.GetChartAsync(chartSubmission.RepresentationId.Value);
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
            chart.SongId = (chartSubmission.SongId ?? songId)!.Value;
            chart.OwnerId = chartSubmission.OwnerId;
            chart.DateUpdated = DateTimeOffset.UtcNow;
            await chartRepository.UpdateChartAsync(chart);
        }

        await notificationService.Notify(owner, null, NotificationType.System, "chart-submission-approval",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await resourceService.GetDisplayName(chartSubmission))
                }
            });

        foreach (var collaboration in await collaborationRepository.GetCollaborationsAsync(
                     new List<string> { "DateCreated" }, new List<bool> { false }, 0, -1,
                     e => e.SubmissionId == chartSubmission.Id && e.Status == RequestStatus.Approved))
        {
            if (await authorshipRepository.AuthorshipExistsAsync(chart.Id, collaboration.InviteeId)) continue;
            var authorship = new Authorship
            {
                ResourceId = chart.Id,
                AuthorId = collaboration.InviteeId,
                Position = collaboration.Position,
                DateCreated = DateTimeOffset.UtcNow
            };
            await authorshipRepository.CreateAuthorshipAsync(authorship);
        }

        if (description != null && mentions != null)
            await notificationService.NotifyMentions(mentions, owner,
                resourceService.GetRichText<Chart>(chart.Id.ToString(), chartSubmission.Description!));
    }

    public async Task RejectChart(ChartSubmission chartSubmission)
    {
        await notificationService.Notify((await userManager.FindByIdAsync(chartSubmission.OwnerId.ToString()))!, null,
            NotificationType.System, "chart-submission-rejection",
            new Dictionary<string, string>
            {
                {
                    "Chart",
                    resourceService.GetRichText<ChartSubmission>(chartSubmission.Id.ToString(),
                        await resourceService.GetDisplayName(chartSubmission))
                }
            });
    }
}