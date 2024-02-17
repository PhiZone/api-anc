using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class MailSenderService(IMailService mailService, IRabbitMqService rabbitMqService, IHostEnvironment env) : BackgroundService
{
    private readonly IModel _channel = rabbitMqService.GetConnection().CreateModel();
    private readonly string _queue = env.IsProduction() ? "email" : "email-dev";

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare(_queue, false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (_, args) =>
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var mailDto = JsonConvert.DeserializeObject<MailTaskDto>(message);
            await mailService.SendMailAsync(mailDto!);
            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume(_queue, false, consumer);

        return Task.CompletedTask;
    }
}