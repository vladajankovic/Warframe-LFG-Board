using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserNotification.Services;

namespace UserNotification.Controllers
{
    [Route("v1/user-notification/notifications")]
    [ApiController]
    [Authorize]
    public class UserNotificationsController : ControllerBase
    {
        private readonly IUserNotificationsService _notificationsService;

        public UserNotificationsController(IUserNotificationsService notificationsService)
        {
            _notificationsService = notificationsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var status = User.FindFirstValue("subscription_status");
            var hasActiveSubscription = string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase);
            var result = await _notificationsService.GetPendingForUser(userId, hasActiveSubscription);

            return Ok(result);
        }

        [HttpPost("{notificationId}/dismiss")]
        public async Task<IActionResult> DismissNotification([FromRoute] string notificationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _notificationsService.Dismiss(userId, notificationId);
            return result ? NoContent() : NotFound();
        }
    }
}
