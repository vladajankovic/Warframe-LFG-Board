using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace UserNotification.Hubs
{
    [Authorize]
    public class UserNotificationHub : Hub
    {
        public const string Route = "/hubs/user-notifications";
        public const string GroupRequestCreatedMethod = "GroupRequestCreated";

        public static string GetUserGroupName(string userId)
        {
            return $"user:{userId}";
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            }

            await base.OnConnectedAsync();
        }
    }
}
