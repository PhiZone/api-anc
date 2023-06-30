using System.Text;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using PhiZoneApi.Dtos;
using PhiZoneApi.Enums;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class MailSenderService : BackgroundService
{
    private readonly IModel _channel;
    private readonly IMailService _mailService;
    private readonly UserManager<User> _userManager;

    public MailSenderService(IMailService mailService, IRabbitMqService rabbitMqService, UserManager<User> userManager)
    {
        _mailService = mailService;
        _userManager = userManager;
        _channel = rabbitMqService.GetConnection().CreateModel();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare("email", false, false, false, null);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, args) =>
        {
            var body = args.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var mailDto = JsonConvert.DeserializeObject<MailDto>(message);

            var result = await _mailService.SendMailAsync(mailDto!);
            if (result == string.Empty)
                switch (mailDto!.SucceedingAction)
                {
                    case SucceedingAction.Create:
                        await _userManager.CreateAsync(mailDto.User);
                        Console.WriteLine($"WOCAO CREATED !!! {mailDto.User.UserName} {mailDto.User.Email}");
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