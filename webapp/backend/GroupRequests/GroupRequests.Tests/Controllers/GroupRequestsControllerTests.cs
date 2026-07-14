using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using GroupRequests.Commands;
using GroupRequests.Controllers;
using GroupRequests.Models;
using GroupRequests.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GroupRequests.Tests.Controllers
{
    public class GroupRequestsControllerTests
    {
        [Fact]
        public async Task CreateGroupRequest_ForwardsSubscriptionStatusToService()
        {
            var serviceMock = new Mock<IGroupRequestsService>();
            var addValidatorMock = new Mock<IValidator<AddGroupRequestCommand>>();
            var deleteValidatorMock = new Mock<IValidator<DeleteGroupRequestCommand>>();
            var loggerMock = new Mock<ILogger<GroupRequestsController>>();
            var mapperMock = new Mock<IMapper>();
            var command = new AddGroupRequestCommand
            {
                InGameName = "PlayerOne",
                ItemName = "Forma Blueprint"
            };
            var created = new GroupRequestDTO
            {
                UserId = "user-1",
                InGameName = command.InGameName,
                ItemName = command.ItemName
            };

            addValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            serviceMock.Setup(s => s.CreateGroupRequest(command, "Active"))
                .ReturnsAsync(created);

            var controller = new GroupRequestsController(
                serviceMock.Object,
                addValidatorMock.Object,
                deleteValidatorMock.Object,
                loggerMock.Object,
                mapperMock.Object)
            {
                ControllerContext = BuildControllerContext("user-1", "Active")
            };

            var result = await controller.CreateGroupRequest(command);

            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);
            Assert.Same(created, objectResult.Value);
            Assert.Equal("user-1", command.UserId);
            serviceMock.Verify(s => s.CreateGroupRequest(command, "Active"), Times.Once);
        }

        [Fact]
        public async Task CreateGroupRequest_WhenSubscriptionClaimIsMissing_ForwardsInactiveStatus()
        {
            var serviceMock = new Mock<IGroupRequestsService>();
            var addValidatorMock = new Mock<IValidator<AddGroupRequestCommand>>();
            var deleteValidatorMock = new Mock<IValidator<DeleteGroupRequestCommand>>();
            var command = new AddGroupRequestCommand
            {
                InGameName = "PlayerOne",
                ItemName = "Forma Blueprint"
            };

            addValidatorMock.Setup(v => v.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());
            serviceMock.Setup(s => s.CreateGroupRequest(command, "Inactive"))
                .ReturnsAsync(new GroupRequestDTO());

            var controller = new GroupRequestsController(
                serviceMock.Object,
                addValidatorMock.Object,
                deleteValidatorMock.Object,
                Mock.Of<ILogger<GroupRequestsController>>(),
                Mock.Of<IMapper>())
            {
                ControllerContext = BuildControllerContext("user-1")
            };

            await controller.CreateGroupRequest(command);

            serviceMock.Verify(s => s.CreateGroupRequest(command, "Inactive"), Times.Once);
        }

        private static ControllerContext BuildControllerContext(string userId, string? subscriptionStatus = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId)
            };

            if (subscriptionStatus != null)
            {
                claims.Add(new Claim("subscription_status", subscriptionStatus));
            }

            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
                }
            };
        }
    }
}
