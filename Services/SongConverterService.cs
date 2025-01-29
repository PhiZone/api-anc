using System.Text;
using PhiZoneApi.Constants;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class SongConverterService(
    IRabbitMqService rabbitMqService,
    ISongService songService,
    ISongRepository songRepository,
    ISongSubmissionRepository songSubmissionRepository,
    IFeishuService feishuService,
    IHostEnvironment env,
    ILogger<SongConverterService> logger) : BackgroundService
{
    private readonly IChannel _channel = rabbitMqService.GetConnection().CreateChannelAsync().Result;
    private readonly string _queue = env.IsProduction() ? "song" : "song-dev";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _channel.QueueDeclareAsync(_queue, false, false, false, null, false, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            if (args.BasicProperties.Headers == null ||
                !args.BasicProperties.Headers.TryGetValue("SongId", out var songIdObj) ||
                !args.BasicProperties.Headers.TryGetValue("IsSubmission", out var isSubmissionObj))
                return;

            var songId = Encoding.UTF8.GetString((byte[])songIdObj!);
            var isSubmission = bool.Parse(Encoding.UTF8.GetString((byte[])isSubmissionObj!));
            var body = args.Body.ToArray();
            if (isSubmission)
            {
                var song = await songSubmissionRepository.GetSongSubmissionAsync(Guid.Parse(songId));
                var result = await songService.UploadAsync(song.Title, body);
                if (result != null)
                {
                    song.File = result.Value.Item1;
                    song.FileChecksum = result.Value.Item2;
                    song.Duration = result.Value.Item3;
                    song.DateFileUpdated = DateTimeOffset.UtcNow;
                    song.DateUpdated = DateTimeOffset.UtcNow;
                    song.Status = RequestStatus.Waiting;

                    if (song.PreviewEnd > song.Duration) song.PreviewEnd = song.Duration.Value;
                    if (song.PreviewStart > song.PreviewEnd) song.PreviewStart = TimeSpan.Zero;

                    await songSubmissionRepository.UpdateSongSubmissionAsync(song);
                    await feishuService.Notify(song, FeishuResources.ContentReviewalChat);
                    logger.LogInformation(LogEvents.SongInfo, "Completed song submission schedule: {Title}",
                        song.Title);
                }
            }
            else
            {
                var song = await songRepository.GetSongAsync(Guid.Parse(songId));
                var result = await songService.UploadAsync(song.Title, body);
                if (result != null)
                {
                    song.File = result.Value.Item1;
                    song.FileChecksum = result.Value.Item2;
                    song.Duration = result.Value.Item3;
                    song.DateFileUpdated = DateTimeOffset.UtcNow;
                    song.DateUpdated = DateTimeOffset.UtcNow;

                    if (song.PreviewEnd > song.Duration) song.PreviewEnd = song.Duration.Value;
                    if (song.PreviewStart > song.PreviewEnd) song.PreviewStart = TimeSpan.Zero;

                    await songRepository.UpdateSongAsync(song);
                    logger.LogInformation(LogEvents.SongInfo, "Completed song schedule: {Title}",
                        song.Title);
                }
            }

            await _channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(_queue, false, consumer, stoppingToken);
    }
}