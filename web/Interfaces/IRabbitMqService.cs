using RabbitMQ.Client;

namespace PhiZoneApi.Interfaces;

public interface IRabbitMqService
{
    IConnection GetConnection();
}