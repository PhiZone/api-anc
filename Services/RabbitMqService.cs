using Microsoft.Extensions.Options;
using PhiZoneApi.Configurations;
using PhiZoneApi.Interfaces;
using RabbitMQ.Client;

namespace PhiZoneApi.Services;

public class RabbitMqService : IRabbitMqService
{
    private readonly IConnection _connection;

    public RabbitMqService(IOptions<RabbitMqSettings> options)
    {
        Console.WriteLine("================= SAKUZYO BEAM =================");
        var settings = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.HostName,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password
        };
        _connection = factory.CreateConnection();
        Console.WriteLine("================= BEAM SAKUZYO =================");
    }

    public IConnection GetConnection()
    {
        return _connection;
    }
}