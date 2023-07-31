using System.Text;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class SongConverterService : BackgroundService
{
    private readonly IModel _channel;
    private readonly ISongRepository _songRepository;
    private readonly ISongSubmissionRepository _songSubmissionRepository;
    private readonly ISongService _songService;

    public SongConverterService(IRabbitMqService rabbitMqService, ISongService songService,
        ISongRepository songRepository, ISongSubmissionRepository songSubmissionRepository)
    {
        _songService = songService;
        _songRepository = songRepository;
        _songSubmissionRepository = songSubmissionRepository;
        _channel = rabbitMqService.GetConnection().CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("song", false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            if (args.BasicProperties.Headers == null ||
                !args.BasicProperties.Headers.TryGetValue("SongId", out var songIdObj) ||
                !args.BasicProperties.Headers.TryGetValue("IsSubmission", out var isSubmissionObj))
                return;

            var songId = Encoding.UTF8.GetString((byte[])songIdObj);
            var isSubmission = bool.Parse(Encoding.UTF8.GetString((byte[])isSubmissionObj));
            var body = args.Body.ToArray();
            if (isSubmission)
            {
                var song = await _songSubmissionRepository.GetSongSubmissionAsync(new Guid(songId));
                var result = await _songService.UploadAsync(song.Title, body);
                if (result != null)
                {
                    song.File = result.Value.Item1;
                    song.FileChecksum = result.Value.Item2;
                    song.Duration = result.Value.Item3;
                    song.DateUpdated = DateTimeOffset.UtcNow;

                    if (song.PreviewEnd > song.Duration) song.PreviewEnd = song.Duration.Value;

                    if (song.PreviewStart > song.PreviewEnd) song.PreviewStart = TimeSpan.Zero;

                    await _songSubmissionRepository.UpdateSongSubmissionAsync(song);
                }
            }
            else
            {
                var song = await _songRepository.GetSongAsync(new Guid(songId));
                var result = await _songService.UploadAsync(song.Title, body);
                if (result != null)
                {
                    song.File = result.Value.Item1;
                    song.FileChecksum = result.Value.Item2;
                    song.Duration = result.Value.Item3;
                    song.DateUpdated = DateTimeOffset.UtcNow;

                    if (song.PreviewEnd > song.Duration) song.PreviewEnd = song.Duration.Value;

                    if (song.PreviewStart > song.PreviewEnd) song.PreviewStart = TimeSpan.Zero;

                    await _songRepository.UpdateSongAsync(song);
                }
            }

            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("song", false, consumer);

        return Task.CompletedTask;
    }
}