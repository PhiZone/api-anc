using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Dtos.Deliverers;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class MailSenderService : BackgroundService
{
    private readonly IModel _channel;
    private readonly IMailService _mailService;
    private readonly IUserService _userService;

    public MailSenderService(IMailService mailService, IRabbitMqService rabbitMqService, IUserService userService)
    {
        _mailService = mailService;
        _userService = userService;
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

            var result = await _mailService.SendMailAsync(mailDto!);
            if (result == string.Empty)
                switch (mailDto!.SucceedingAction)
                {
                    case SucceedingAction.Create:
                        await _userService.CreateUser(mailDto.User);
                        break;
                    case SucceedingAction.Update:
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(SucceedingAction), "Unsupported value.");
                }

            _channel.BasicAck(args.DeliveryTag, false);
        };

        _channel.BasicConsume("email", false, consumer);

        return Task.CompletedTask;
    }
}