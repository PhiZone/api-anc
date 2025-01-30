using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class MailSenderService(IMailService mailService, IRabbitMqService rabbitMqService, IHostEnvironment env)
    : BackgroundService
{
    private readonly IChannel _channel = rabbitMqService.GetConnection().CreateChannelAsync().Result;
    private readonly string _queue = env.IsProduction() ? "email" : "email-dev";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _channel.QueueDeclareAsync(_queue, false, false, false, null, false, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var mailDto = JsonConvert.DeserializeObject<MailTaskDto>(message);
            await mailService.SendMailAsync(mailDto!);
            await _channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(_queue, false, consumer, stoppingToken);
    }
}