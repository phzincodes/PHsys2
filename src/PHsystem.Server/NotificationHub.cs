using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PHsystem.Server;

[Authorize]
public sealed class NotificationHub : Hub
{
    // No client-supplied group joins and no remote execution methods.
}
