using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class MailSenderService : BackgroundService
{
    private readonly IModel _channel;
    private readonly IMailService _mailService;

    public MailSenderService(IMailService mailService, IRabbitMqService rabbitMqService)
    {
        _mailService = mailService;
        _channel = rabbitMqService.GetConnection().CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("email", false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var mailDto = JsonConvert.DeserializeObject<MailTaskDto>(message);
            await _mailService.SendMailAsync(mailDto!);
            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("email", false, consumer);

        return Task.CompletedTask;
    }
}