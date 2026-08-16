using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using UsersDirectory.Controllers;
using UsersDirectory.Models;
using UsersDirectory.Services;

namespace UsersDirectory.Tests.Controllers
{
    public class WarframeProfileControllerTests
    {
        private readonly Mock<IWarframeProfileService> _serviceMock = new();
        private readonly WarframeProfileController _controller;

        public WarframeProfileControllerTests()
        {
            _controller = new WarframeProfileController(
                _serviceMock.Object,
                Mock.Of<ILogger<WarframeProfileController>>());
        }

        [Fact]
        public async Task GetProfile_WhenUpstreamSucceeds_ReturnsProfile()
        {
            var profile = new WarframeProfileData
            {
                DisplayName = "UndeadWinter"
            };
            _serviceMock
                .Setup(service => service.GetProfile("player-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            var result = await _controller.GetProfile("player-1", CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(profile, okResult.Value);
        }

        [Fact]
        public async Task GetProfile_WhenUpstreamFails_ReturnsBadGateway()
        {
            _serviceMock
                .Setup(service => service.GetProfile("player-1", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Upstream failed"));

            var result = await _controller.GetProfile("player-1", CancellationToken.None);

            var errorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(502, errorResult.StatusCode);
        }
    }
}
