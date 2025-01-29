using System.Text;
using Newtonsoft.Json;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;
using PhiZoneApi.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PhiZoneApi.Services;

public class TapRecordService(
    IRabbitMqService rabbitMqService,
    ITapGhostService tapGhostService,
    IHostEnvironment env,
    ILogger<TapRecordService> logger) : BackgroundService
{
    private readonly IChannel _channel = rabbitMqService.GetConnection().CreateChannelAsync().Result;
    private readonly string _queue = env.IsProduction() ? "tap-record" : "tap-record-dev";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _channel.QueueDeclareAsync(_queue, false, false, false, null, false, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, args) =>
        {
            if (args.BasicProperties.Headers == null ||
                !args.BasicProperties.Headers.TryGetValue("AppId", out var appIdObj) ||
                !args.BasicProperties.Headers.TryGetValue("Id", out var idObj) ||
                !args.BasicProperties.Headers.TryGetValue("IsChartRanked", out var isChartRankedObj) ||
                !args.BasicProperties.Headers.TryGetValue("ExpDelta", out var experienceDeltaObj))
                return;

            try
            {
                var appId = Guid.Parse(Encoding.UTF8.GetString((byte[])appIdObj!));
                var id = Encoding.UTF8.GetString((byte[])idObj!);
                var isChartRanked = bool.Parse(Encoding.UTF8.GetString((byte[])isChartRankedObj!));
                var experienceDelta = ulong.Parse(Encoding.UTF8.GetString((byte[])experienceDeltaObj!));
                var record = JsonConvert.DeserializeObject<Record>(Encoding.UTF8.GetString(args.Body.ToArray()))!;

                var ghost = await tapGhostService.GetGhost(appId, id);
                var rks = await tapGhostService.CreateRecord(appId, id, record, isChartRanked);
                if (ghost == null) return;
                ghost.Experience += experienceDelta;
                ghost.Rks = rks;
                await tapGhostService.ModifyGhost(ghost);
            }
            catch (Exception e)
            {
                logger.LogError(LogEvents.TapGhostFailure, e, "Failed to upload record for TapTap Ghost");
            }

            await _channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
        };

        await _channel.BasicConsumeAsync(_queue, false, consumer, stoppingToken);
    }
}