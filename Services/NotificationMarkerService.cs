using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class NotificationMarkerService : BackgroundService
{
    private readonly IModel _channel;
    private readonly INotificationRepository _notificationRepository;

    public NotificationMarkerService(IRabbitMqService rabbitMqService, INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
        _channel = rabbitMqService.GetConnection().CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("notification", false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            if (args.BasicProperties.Headers == null ||
                !args.BasicProperties.Headers.TryGetValue("DateRead", out var dateReadObj))
                return;

            var dateRead = DateTimeOffset.Parse(Encoding.UTF8.GetString((byte[])dateReadObj));
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var notificationIds = JsonConvert.DeserializeObject<IEnumerable<Guid>>(message)!;

            var notifications = await _notificationRepository.GetNotificationsAsync("DateCreated", false, 0, -1, null,
                e => notificationIds.Contains(e.Id));

            foreach (var notification in notifications)
            {
                notification.DateRead = dateRead;
            }

            await _notificationRepository.UpdateNotificationsAsync(notifications);

            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("notification", false, consumer);

        return Task.CompletedTask;
    }
}