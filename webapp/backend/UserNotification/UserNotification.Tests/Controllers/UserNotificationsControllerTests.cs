using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UserNotification.Controllers;
using UserNotification.Models;
using UserNotification.Services;
using Xunit;

namespace UserNotification.Tests.Controllers
{
    public class UserNotificationsControllerTests
    {
        private readonly Mock<IUserNotificationsService> _serviceMock;
        private readonly UserNotificationsController _controller;

        public UserNotificationsControllerTests()
        {
            _serviceMock = new Mock<IUserNotificationsService>();
            _controller = new UserNotificationsController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetPendingNotifications_WhenUserIdClaimMissing_ReturnsUnauthorized()
        {
            SetUser([]);

            var result = await _controller.GetPendingNotifications();

            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetPendingNotifications_WhenSubscriptionActive_ReturnsNotifications()
        {
            var notifications = new List<GroupRequestNotification>
            {
                new()
                {
                    Id = "notification-1",
                    GroupRequestUserId = "host-1",
                    ItemName = "Forma",
                    InGameName = "Host",
                    CreatedAt = DateTime.UtcNow
                }
            };
            SetUser([
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("subscription_status", "Active")
            ]);
            _serviceMock
                .Setup(s => s.GetPendingForUser("user-1", true))
                .ReturnsAsync(notifications);

            var result = await _controller.GetPendingNotifications();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Same(notifications, ok.Value);
        }

        [Fact]
        public async Task GetPendingNotifications_WhenSubscriptionInactive_PassesInactiveFlagToService()
        {
            SetUser([
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim("subscription_status", "Inactive")
            ]);
            _serviceMock
                .Setup(s => s.GetPendingForUser("user-1", false))
                .ReturnsAsync([]);

            var result = await _controller.GetPendingNotifications();

            Assert.IsType<OkObjectResult>(result);
            _serviceMock.Verify(s => s.GetPendingForUser("user-1", false), Times.Once);
        }

        [Fact]
        public async Task DismissNotification_WhenDismissSucceeds_ReturnsNoContent()
        {
            SetUser([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            _serviceMock
                .Setup(s => s.Dismiss("user-1", "notification-1"))
                .ReturnsAsync(true);

            var result = await _controller.DismissNotification("notification-1");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DismissNotification_WhenNotificationDoesNotExist_ReturnsNotFound()
        {
            SetUser([new Claim(ClaimTypes.NameIdentifier, "user-1")]);
            _serviceMock
                .Setup(s => s.Dismiss("user-1", "missing"))
                .ReturnsAsync(false);

            var result = await _controller.DismissNotification("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DismissNotification_WhenUserIdClaimMissing_ReturnsUnauthorized()
        {
            SetUser([]);

            var result = await _controller.DismissNotification("notification-1");

            Assert.IsType<UnauthorizedResult>(result);
        }

        private void SetUser(IEnumerable<Claim> claims)
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
                }
            };
        }
    }
}
