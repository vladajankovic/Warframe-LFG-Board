using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using UserNotification.Commands;
using UserNotification.Controllers;
using UserNotification.Services;
using Xunit;

namespace UserNotification.Tests.Controllers
{
    public class UserFavoritesControllerTests
    {
        [Fact]
        public async Task AddFavorite_WhenLimitIsReached_ReturnsBadRequestWithServiceMessage()
        {
            var serviceMock = new Mock<IUserFavoritesService>();
            serviceMock.Setup(s => s.Create(It.Is<AddOrRemoveUserFavoritesCommand>(command =>
                    command.UserId == "user-1" && command.ItemName == "Forma")))
                .ThrowsAsync(new InvalidOperationException("Favorite limit reached"));
            var controller = BuildController(serviceMock.Object, "Active");

            var result = await controller.AddFavoriteItemForNotification("Forma");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Favorite limit reached", badRequest.Value);
        }

        [Fact]
        public async Task AddFavorite_WhenSubscriptionIsInactive_DoesNotCallService()
        {
            var serviceMock = new Mock<IUserFavoritesService>();
            var controller = BuildController(serviceMock.Object, "Inactive");

            var result = await controller.AddFavoriteItemForNotification("Forma");

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("You need an active subscription to listen for notifications for favorite items.", badRequest.Value);
            serviceMock.Verify(
                s => s.Create(It.IsAny<AddOrRemoveUserFavoritesCommand>()),
                Times.Never);
        }

        private static UserFavoritesController BuildController(
            IUserFavoritesService service,
            string subscriptionStatus)
        {
            return new UserFavoritesController(service)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, "user-1"),
                            new Claim("subscription_status", subscriptionStatus)
                        ], "Test"))
                    }
                }
            };
        }
    }
}
