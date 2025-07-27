using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PhiZoneApi.Constants;
using PhiZoneApi.Data;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;

// ReSharper disable InvertIf

namespace PhiZoneApi.Services;

public class DataConsistencyMaintainer(IServiceProvider serviceProvider, ILogger<DataConsistencyMaintainer> logger)
    : IHostedService, IDisposable
{
    private CancellationToken _cancellationToken;
    private Timer? _timer;

    public void Dispose()
    {
        _timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _timer = new Timer(Check, null, TimeSpan.Zero, TimeSpan.FromDays(1));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    private async void Check(object? state)
    {
        try
        {
            Cleanup();

            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var seekTuneService = scope.ServiceProvider.GetRequiredService<ISeekTuneService>();
            var recordRepository = scope.ServiceProvider.GetRequiredService<IRecordRepository>();
            var recordService = scope.ServiceProvider.GetRequiredService<IRecordService>();
            var voteRepository = scope.ServiceProvider.GetRequiredService<IVoteRepository>();
            var voteService = scope.ServiceProvider.GetRequiredService<IVoteService>();
            var culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            culture.NumberFormat.PercentPositivePattern = 1;

            await seekTuneService.SyncFingerprints(context, _cancellationToken);

            foreach (var song in await context.SongSubmissions.ToListAsync(_cancellationToken))
            {
                var update = false;
                if (song.DateFileUpdated == default)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Song Submission \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        song.Title, nameof(song.DateFileUpdated), song.DateFileUpdated, song.DateUpdated);
                    song.DateFileUpdated = song.DateUpdated;
                    update = true;
                }

                if (update) context.SongSubmissions.Update(song);
            }

            foreach (var chart in await context.ChartSubmissions.Include(e => e.Song)
                         .Include(e => e.SongSubmission)
                         .ToListAsync(_cancellationToken))
            {
                var update = false;
                if (chart.DateFileUpdated == default)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart Submission \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song?.Title ?? chart.SongSubmission?.Title, chart.Level,
                        Math.Floor(chart.Difficulty), nameof(chart.DateFileUpdated), chart.DateFileUpdated,
                        chart.DateUpdated);
                    chart.DateFileUpdated = chart.DateUpdated;
                    update = true;
                }

                if (update) context.ChartSubmissions.Update(chart);
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var record in await context.Records.Include(e => e.Chart).ToListAsync(_cancellationToken))
            {
                var update = false;
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == record.Id, _cancellationToken);
                var score = recordService.CalculateScore(record.Perfect, record.GoodEarly + record.GoodLate, record.Bad,
                    record.Miss, record.MaxCombo);
                var accuracy = recordService.CalculateAccuracy(record.Perfect, record.GoodEarly + record.GoodLate,
                    record.Bad, record.Miss);
                if (record.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} != {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.LikeCount),
                        record.LikeCount, likeCount);
                    record.LikeCount = likeCount;
                    update = true;
                }

                if (record.Score != score)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} != {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.Score), record.Score,
                        score);
                    record.Score = score;
                    update = true;
                }

                if (Math.Abs(record.Accuracy - accuracy) > 1e-8)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} != {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.Accuracy), record.Accuracy,
                        accuracy);
                    record.Accuracy = accuracy;
                    update = true;
                }

                if (record.StdDeviation < 0.1)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} < {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.StdDeviation),
                        record.StdDeviation, 0.1);
                    if (record.StdDeviation > 0)
                        record.StdDeviation *= 1000;
                    else
                        record.StdDeviation = 40;
                    update = true;
                }

                if (record.StdDeviation > record.GoodJudgment)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} > {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.StdDeviation),
                        record.StdDeviation, record.GoodJudgment);
                    record.StdDeviation = 40;
                    update = true;
                }

                var rksFactor = recordService.CalculateRksFactor(record.PerfectJudgment, record.GoodJudgment);
                var rks = recordService.CalculateRks(record.Perfect, record.GoodEarly + record.GoodLate, record.Bad,
                    record.Miss, record.Chart.Difficulty, record.StdDeviation) * rksFactor;
                if (Math.Abs(record.Rks - rks) > 1e-8)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Record \"{Score} {Accuracy}\" on its {Property}: {Actual} != {Expected}",
                        record.Score, record.Accuracy.ToString("P2", culture), nameof(record.Rks), record.Rks, rks);
                    record.Rks = rks;
                    update = true;
                }

                if (update) context.Records.Update(record);
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var user in await context.Users.ToListAsync(_cancellationToken))
            {
                var update = false;
                var followeeCount = await context.UserRelations.CountAsync(
                    e => e.FollowerId == user.Id && e.Type != UserRelationType.Blacklisted, _cancellationToken);
                var followerCount = await context.UserRelations.CountAsync(
                    e => e.FolloweeId == user.Id && e.Type != UserRelationType.Blacklisted, _cancellationToken);
                var phi3Rks = (await recordRepository.GetRecordsAsync(["Rks"], [true], 0, 3,
                    r => r.OwnerId == user.Id && r.Score == 1000000 && r.Chart.IsRanked)).Sum(r => r.Rks);
                var best27Rks = (await recordRepository.GetPersonalBests(user.Id)).Sum(r => r.Rks);
                var rks = (phi3Rks + best27Rks) / 30;
                if (user.FolloweeCount != followeeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for User #{Id} \"{UserName}\" on {Pronoun} {Property}: {Actual} != {Expected}",
                        user.Id, user.UserName, user.Gender switch
                        {
                            Gender.Male => "his",
                            Gender.Female => "her",
                            _ => "their"
                        }, nameof(user.FolloweeCount), user.FolloweeCount, followeeCount);
                    user.FolloweeCount = followeeCount;
                    update = true;
                }

                if (user.FollowerCount != followerCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for User \"{UserName}\" on {Pronoun} {Property}: {Actual} != {Expected}",
                        user.UserName, user.Gender switch
                        {
                            Gender.Male => "his",
                            Gender.Female => "her",
                            _ => "their"
                        }, nameof(user.FollowerCount), user.FollowerCount, followerCount);
                    user.FollowerCount = followerCount;
                    update = true;
                }

                if (Math.Abs(user.Rks - rks) > 1e-8)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for User \"{UserName}\" on {Pronoun} {Property}: {Actual} != {Expected}",
                        user.UserName, user.Gender switch
                        {
                            Gender.Male => "his",
                            Gender.Female => "her",
                            _ => "their"
                        }, nameof(user.Rks), user.Rks, rks);
                    user.Rks = rks;
                    update = true;
                }

                if (update) context.Users.Update(user);
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var chart in await context.Charts.Include(e => e.Song).ToListAsync(_cancellationToken))
            {
                var update = false;
                var playCount = await context.Records.CountAsync(e => e.ChartId == chart.Id, _cancellationToken);
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == chart.Id, _cancellationToken);
                var votes = await voteRepository.GetVotesAsync(predicate: vote => vote.ChartId == chart.Id);
                var amount = votes.Sum(vote => vote.Multiplier);
                var r = voteService.GetReliability(amount);
                var score = votes.Sum(vote => vote.Total * vote.Multiplier) / 6;
                var rating = voteService.GetRating(chart.Score, amount, r);
                var ratingOnArrangement =
                    voteService.GetRating(votes.Sum(vote => vote.Arrangement * vote.Multiplier), amount, r);
                var ratingOnGameplay =
                    voteService.GetRating(votes.Sum(vote => vote.Gameplay * vote.Multiplier), amount, r);
                var ratingOnVisualEffects =
                    voteService.GetRating(votes.Sum(vote => vote.VisualEffects * vote.Multiplier), amount, r);
                var ratingOnCreativity =
                    voteService.GetRating(votes.Sum(vote => vote.Creativity * vote.Multiplier), amount, r);
                var ratingOnConcord =
                    voteService.GetRating(votes.Sum(vote => vote.Concord * vote.Multiplier), amount, r);
                var ratingOnImpression =
                    voteService.GetRating(votes.Sum(vote => vote.Impression * vote.Multiplier), amount, r);
                if (chart.PlayCount != playCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.PlayCount), chart.PlayCount, playCount);
                    chart.PlayCount = playCount;
                    update = true;
                }

                if (chart.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.LikeCount), chart.LikeCount, likeCount);
                    chart.LikeCount = likeCount;
                    update = true;
                }

                if (Math.Abs(chart.Score - score) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty), nameof(chart.Score),
                        chart.Score, score);
                    chart.Score = score;
                    update = true;
                }

                if (Math.Abs(chart.Rating - rating) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.Rating), chart.Rating, rating);
                    chart.Rating = rating;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnArrangement - ratingOnArrangement) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnArrangement), chart.RatingOnArrangement, ratingOnArrangement);
                    chart.RatingOnArrangement = ratingOnArrangement;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnGameplay - ratingOnGameplay) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnGameplay), chart.RatingOnGameplay, ratingOnGameplay);
                    chart.RatingOnGameplay = ratingOnGameplay;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnVisualEffects - ratingOnVisualEffects) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnVisualEffects), chart.RatingOnVisualEffects, ratingOnVisualEffects);
                    chart.RatingOnVisualEffects = ratingOnVisualEffects;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnCreativity - ratingOnCreativity) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnCreativity), chart.RatingOnCreativity, ratingOnCreativity);
                    chart.RatingOnCreativity = ratingOnCreativity;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnConcord - ratingOnConcord) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnConcord), chart.RatingOnConcord, ratingOnConcord);
                    chart.RatingOnConcord = ratingOnConcord;
                    update = true;
                }

                if (Math.Abs(chart.RatingOnImpression - ratingOnImpression) > 1e-7)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.RatingOnImpression), chart.RatingOnImpression, ratingOnImpression);
                    chart.RatingOnImpression = ratingOnImpression;
                    update = true;
                }

                if (chart.DateFileUpdated == default)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chart \"{Title} [{Level} {Difficulty}]\" on its {Property}: {Actual} != {Expected}",
                        chart.Title ?? chart.Song.Title, chart.Level, Math.Floor(chart.Difficulty),
                        nameof(chart.DateFileUpdated), chart.DateFileUpdated, chart.DateUpdated);
                    chart.DateFileUpdated = chart.DateUpdated;
                    update = true;
                }

                if (update) context.Charts.Update(chart);
            }

            foreach (var comment in await context.Comments.ToListAsync(_cancellationToken))
            {
                var update = false;
                var replyCount = await context.Replies.CountAsync(e => e.CommentId == comment.Id, _cancellationToken);
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == comment.Id, _cancellationToken);
                if (comment.ReplyCount != replyCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Comment \"{Content}\" on its {Property}: {Actual} != {Expected}",
                        comment.Content.Length > 10 ? $"{comment.Content[..10]}..." : comment.Content,
                        nameof(comment.ReplyCount), comment.ReplyCount, replyCount);
                    comment.ReplyCount = replyCount;
                    update = true;
                }

                if (comment.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Comment \"{Content}\" on its {Property}: {Actual} != {Expected}",
                        comment.Content.Length > 10 ? $"{comment.Content[..10]}..." : comment.Content,
                        nameof(comment.LikeCount), comment.LikeCount, likeCount);
                    comment.LikeCount = likeCount;
                    update = true;
                }

                if (update) context.Comments.Update(comment);
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var collection in await context.Collections.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == collection.Id, _cancellationToken);
                if (collection.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Collection \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        collection.Title, nameof(collection.LikeCount), collection.LikeCount, likeCount);
                    collection.LikeCount = likeCount;
                    context.Collections.Update(collection);
                }
            }

            foreach (var chapter in await context.Chapters.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == chapter.Id, _cancellationToken);
                if (chapter.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Chapter \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        chapter.Title, nameof(chapter.LikeCount), chapter.LikeCount, likeCount);
                    chapter.LikeCount = likeCount;
                    context.Chapters.Update(chapter);
                }
            }

            foreach (var announcement in await context.Announcements.ToListAsync(_cancellationToken))
            {
                var likeCount =
                    await context.Likes.CountAsync(e => e.ResourceId == announcement.Id, _cancellationToken);
                if (announcement.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Announcement \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        announcement.Title, nameof(announcement.LikeCount), announcement.LikeCount, likeCount);
                    announcement.LikeCount = likeCount;
                    context.Announcements.Update(announcement);
                }
            }

            foreach (var song in await context.Songs.ToListAsync(_cancellationToken))
            {
                var update = false;
                var playCount = await context.Records.CountAsync(e => e.Chart.SongId == song.Id, _cancellationToken);
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == song.Id, _cancellationToken);
                if (song.PlayCount != playCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Song \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        song.Title, nameof(song.PlayCount), song.PlayCount, playCount);
                    song.PlayCount = playCount;
                    update = true;
                }

                if (song.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Song \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        song.Title, nameof(song.LikeCount), song.LikeCount, likeCount);
                    song.LikeCount = likeCount;
                    update = true;
                }

                if (song.DateFileUpdated == default)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Song \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        song.Title, nameof(song.DateFileUpdated), song.DateFileUpdated, song.DateUpdated);
                    song.DateFileUpdated = song.DateUpdated;
                    update = true;
                }

                if (update) context.Songs.Update(song);
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var application in await context.Applications.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == application.Id, _cancellationToken);
                if (application.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Application \"{Name}\" on its {Property}: {Actual} != {Expected}",
                        application.Name, nameof(application.LikeCount), application.LikeCount, likeCount);
                    application.LikeCount = likeCount;
                    context.Applications.Update(application);
                }
            }

            foreach (var reply in await context.Replies.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == reply.Id, _cancellationToken);
                if (reply.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Reply \"{Content}\" on its {Property}: {Actual} != {Expected}",
                        reply.Content.Length > 10 ? $"{reply.Content[..10]}..." : reply.Content,
                        nameof(reply.LikeCount), reply.LikeCount, likeCount);
                    reply.LikeCount = likeCount;
                    context.Replies.Update(reply);
                }
            }

            await context.SaveChangesAsync(_cancellationToken);

            foreach (var eventEntity in await context.Events.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == eventEntity.Id, _cancellationToken);
                if (eventEntity.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Event \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        eventEntity.Title, nameof(eventEntity.LikeCount), eventEntity.LikeCount, likeCount);
                    eventEntity.LikeCount = likeCount;
                    context.Events.Update(eventEntity);
                }
            }

            foreach (var eventDivision in await context.EventDivisions.ToListAsync(_cancellationToken))
            {
                var likeCount =
                    await context.Likes.CountAsync(e => e.ResourceId == eventDivision.Id, _cancellationToken);
                if (eventDivision.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Event Division \"{Title}\" on its {Property}: {Actual} != {Expected}",
                        eventDivision.Title, nameof(eventDivision.LikeCount), eventDivision.LikeCount, likeCount);
                    eventDivision.LikeCount = likeCount;
                    context.EventDivisions.Update(eventDivision);
                }
            }

            foreach (var eventTeam in await context.EventTeams.ToListAsync(_cancellationToken))
            {
                var likeCount = await context.Likes.CountAsync(e => e.ResourceId == eventTeam.Id, _cancellationToken);
                if (eventTeam.LikeCount != likeCount)
                {
                    logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                        "Found inconsistency for Event Team \"{Name}\" on its {Property}: {Actual} != {Expected}",
                        eventTeam.Name, nameof(eventTeam.LikeCount), eventTeam.LikeCount, likeCount);
                    eventTeam.LikeCount = likeCount;
                    context.EventTeams.Update(eventTeam);
                }
            }

            await context.SaveChangesAsync(_cancellationToken);
            logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                "Data consistency has been checked successfully");
        }
        catch (Exception e)
        {
            logger.LogWarning(LogEvents.DataConsistencyMaintenance, e,
                "An error has occurred whilst checking for data consistency");
        }
    }

    private void Cleanup()
    {
        for (var i = -2; i >= -7; i--)
        {
            var directory = Path.Combine(Path.GetTempPath(),
                $"PZSubmissionSaves{DateTimeOffset.UtcNow.AddDays(i):yyyyMMdd}");
            if (Path.Exists(directory))
            {
                Directory.Delete(directory, true);
                logger.LogInformation(LogEvents.DataConsistencyMaintenance,
                    "Deleted temporary directory {Directory}", directory);
            }
        }
    }
}