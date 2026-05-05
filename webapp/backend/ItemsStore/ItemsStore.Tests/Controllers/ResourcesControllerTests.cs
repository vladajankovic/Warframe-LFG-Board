using ItemsStore.Commands;
using ItemsStore.Controllers;
using ItemsStore.Models;
using ItemsStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ItemsStore.Tests.Controllers
{
    public class ResourcesControllerTests
    {
        private readonly Mock<IResourceService> _serviceMock;
        private readonly ResourcesController _controller;

        public ResourcesControllerTests()
        {
            _serviceMock = new Mock<IResourceService>();
            var loggerMock = new Mock<ILogger<ResourcesController>>();
            _controller = new ResourcesController(_serviceMock.Object, loggerMock.Object);
        }

        private static Resource MakeResource(string name = "Ferrite", string description = "A common resource.") =>
            new() { Id = 1, Name = name, Description = description };

        private static AddResourceCommand MakeCommand(string name = "Ferrite", string description = "A common resource.") =>
            new() { Name = name, Description = description };

        // ── GET all ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllResources_Returns200WithList()
        {
            var resources = new List<Resource> { MakeResource() };
            _serviceMock.Setup(s => s.GetAllResources()).ReturnsAsync(resources);

            var result = await _controller.GetAllResources();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resources, ok.Value);
        }

        // ── GET single ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetResource_WhenFound_Returns200()
        {
            var resource = MakeResource();
            _serviceMock.Setup(s => s.GetResource("ferrite")).ReturnsAsync(resource);

            var result = await _controller.GetResource("ferrite");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(resource, ok.Value);
        }

        [Fact]
        public async Task GetResource_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetResource(It.IsAny<string>())).ReturnsAsync((Resource?)null);

            var result = await _controller.GetResource("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── POST ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddNewResource_Returns201WithCreatedResource()
        {
            var command = MakeCommand();
            var created = MakeResource();
            _serviceMock.Setup(s => s.CreateResource(command)).ReturnsAsync(created);

            var result = await _controller.AddNewResource(command);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, status.StatusCode);
            Assert.Equal(created, status.Value);
        }

        [Fact]
        public async Task AddNewResource_WhenAlreadyExists_Returns400()
        {
            _serviceMock.Setup(s => s.CreateResource(It.IsAny<AddResourceCommand>()))
                .ThrowsAsync(new InvalidOperationException("Resource already exists"));

            var result = await _controller.AddNewResource(MakeCommand());

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ── DELETE ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteResource_WhenDeleted_Returns204()
        {
            _serviceMock.Setup(s => s.DeleteResource("ferrite")).ReturnsAsync(true);

            var result = await _controller.DeleteResource("ferrite");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteResource_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteResource(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _controller.DeleteResource("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── PUT ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateResource_WhenUpdated_Returns200()
        {
            var command = MakeCommand();
            var updated = MakeResource();
            _serviceMock.Setup(s => s.UpdateResource(command)).ReturnsAsync(updated);

            var result = await _controller.UpdateResource(command);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updated, ok.Value);
        }

        [Fact]
        public async Task UpdateResource_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateResource(It.IsAny<AddResourceCommand>()))
                .ThrowsAsync(new InvalidOperationException("Not found"));

            var result = await _controller.UpdateResource(MakeCommand());

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
