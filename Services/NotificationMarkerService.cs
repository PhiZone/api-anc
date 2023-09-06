using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class NotificationMarkerService : BackgroundService
{
    private readonly IModel _channel;
    private readonly IServiceProvider _serviceProvider;

    public NotificationMarkerService(IServiceProvider serviceProvider, IRabbitMqService rabbitMqService)
    {
        _serviceProvider = serviceProvider;
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

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            var dateRead = DateTimeOffset.Parse(Encoding.UTF8.GetString((byte[])dateReadObj));
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var notificationIds = JsonConvert.DeserializeObject<IEnumerable<Guid>>(message)!;

            await using var scope = _serviceProvider.CreateAsyncScope();
            var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
            var notifications = await notificationRepository.GetNotificationsAsync("DateCreated", false, 0, -1, null,
                e => notificationIds.Contains(e.Id));

            foreach (var notification in notifications) notification.DateRead = dateRead;

            await notificationRepository.UpdateNotificationsAsync(notifications);

            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("notification", false, consumer);

        return Task.CompletedTask;
    }
}