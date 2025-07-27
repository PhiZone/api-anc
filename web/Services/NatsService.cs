using NATS.Net;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Services;

public class NatsService(IConfiguration config) : INatsService
{
    private readonly NatsClient _client = new(config["NatsUrl"] ?? "nats://localhost:4222");

    public NatsClient GetClient()
    {
        return _client;
    }
}