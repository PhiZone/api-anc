using Microsoft.AspNetCore.SignalR;
using PhiZoneApi.Constants;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Hubs;

public class SubmissionHub(IResourceService resourceService, ILogger<SubmissionHub> logger) : Hub<ISubmissionClient>
{
    private readonly Dictionary<string, Guid> _userGroupDictionary = new();

    public async Task Register(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        _userGroupDictionary.Add(Context.ConnectionId, sessionId);
        logger.LogInformation(LogEvents.SubmissionHubInfo, "Registered user {ConnectionId} with session {SessionId}",
            Context.ConnectionId, sessionId);
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        // ReSharper disable once InvertIf
        if (_userGroupDictionary.TryGetValue(Context.ConnectionId, out var sessionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId.ToString());
            await resourceService.CleanupSession(sessionId);
            _userGroupDictionary.Remove(Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}