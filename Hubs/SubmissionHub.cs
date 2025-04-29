using Microsoft.AspNetCore.SignalR;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Hubs;

public class SubmissionHub(IResourceService resourceService, ILogger<SubmissionHub> logger) : Hub<ISubmissionClient>
{
    public async Task Register(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        resourceService.JoinSession(Context.ConnectionId, sessionId);
        logger.LogInformation(LogEvents.SubmissionHubInfo, "Registered user {ConnectionId} with session {SessionId}",
            Context.ConnectionId, sessionId);
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        var sessionId = resourceService.GetSessionId(Context.ConnectionId);
        // ReSharper disable once InvertIf
        if (sessionId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.Value.ToString());
            await resourceService.CleanupSession(sessionId.Value);
            resourceService.LeaveSession(Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}