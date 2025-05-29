using NATS.Net;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class NatsService : INatsService
{
    private readonly NatsClient _client;

    public NatsService(IConfiguration config)
    {
        _client = new NatsClient(config["NatsUrl"] ?? "nats://localhost:4222");
    }

    public NatsClient GetClient()
    {
        return _client;
    }
}