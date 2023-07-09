using System.Text;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class SongConverterService : BackgroundService
{
    private readonly IModel _channel;
    private readonly ISongRepository _songRepository;
    private readonly ISongService _songService;

    public SongConverterService(IRabbitMqService rabbitMqService, ISongService songService,
        ISongRepository songRepository)
    {
        _songService = songService;
        _songRepository = songRepository;
        _channel = rabbitMqService.GetConnection().CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("song", false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            if (args.BasicProperties.Headers == null ||
                !args.BasicProperties.Headers.TryGetValue("SongId", out var songIdObj))
                return;

            var songId = Encoding.UTF8.GetString((byte[])songIdObj);
            var body = args.Body.ToArray();
            var song = await _songRepository.GetSongAsync(new Guid(songId));
            var result = await _songService.UploadAsync(song.Title, body);
            if (result != null)
            {
                song.File = result.Value.Item1;
                song.Duration = result.Value.Item2;
                await _songRepository.UpdateSongAsync(song);
            }

            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("song", false, consumer);

        return Task.CompletedTask;
    }
}