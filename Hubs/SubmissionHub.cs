using Microsoft.AspNetCore.SignalR;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Hubs;

public class SubmissionHub(IResourceService resourceService) : Hub<ISubmissionClient>
{
    private readonly Dictionary<string, Guid> _userGroupDictionary = new();

    public async Task Register(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
        _userGroupDictionary[Context.ConnectionId] = sessionId;
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, _userGroupDictionary[Context.ConnectionId].ToString());
        await resourceService.CleanupSession(_userGroupDictionary[Context.ConnectionId]);
        _userGroupDictionary.Remove(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}