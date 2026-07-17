using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SmartFollowUp.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // لما يوزر يعمل Connect
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                // كل يوزر يدخل في Group باسم userId بتاعه
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnConnectedAsync();
        }

        // لما يوزر يعمل Disconnect
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}