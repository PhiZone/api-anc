using Microsoft.AspNetCore.SignalR;
using PhiZoneApi.Interfaces;

namespace PhiZoneApi.Hubs;

public class SubmissionHub : Hub<ISubmissionClient>
{
    public async Task Register(Guid sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId.ToString());
    }
}