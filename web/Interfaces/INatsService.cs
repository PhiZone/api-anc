using NATS.Net;

namespace PhiZoneApi.Interfaces;

public interface INatsService
{
    NatsClient GetClient();
}