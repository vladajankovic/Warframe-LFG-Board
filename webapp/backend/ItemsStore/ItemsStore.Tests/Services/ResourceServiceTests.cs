using AutoMapper;
using ItemsStore.Broker.RabbitMQ;
using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Repository;
using ItemsStore.Services;
using ItemsStore.Utility;
using ItemsStore.Utility.Mappers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ItemsStore.Tests.Services
{
    public class ResourceServiceTests
    {
        private readonly Mock<IResourceRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<ResourceService>> _loggerMock;
        private readonly Mock<IRabbitMQPublisher> _brokerMock;
        private readonly ResourceService _service;

        public ResourceServiceTests()
        {
            _repoMock = new Mock<IResourceRepository>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = config.CreateMapper();
            _loggerMock = new Mock<ILogger<ResourceService>>();
            _brokerMock = new Mock<IRabbitMQPublisher>();
            _service = new ResourceService(_repoMock.Object, _mapper, _loggerMock.Object, _brokerMock.Object);
        }

        private static ResourceEntity MakeEntity(
            string name = "Ferrite",
            string? description = "A common resource.",
            string? region = "Star Chart",
            string? category = "Minerals",
            string? group = "Common") =>
            new() { Id = 1, ResourceName = name.ToSnakeCase(), Name = name, Description = description, Region = region, Category = category, Group = group };

        private static AddResourceCommand MakeCommand(
            string name = "Ferrite",
            string description = "A common resource.",
            string region = "Star Chart",
            string category = "Minerals",
            string group = "Common") =>
            new() { Name = name, Description = description, Region = region, Category = category, Group = group };

        // ── CreateResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateResource_WhenResourceDoesNotExist_ReturnsCreatedResource()
        {
            var command = MakeCommand();
            var entity = MakeEntity();

            _repoMock.Setup(r => r.GetResource(command.Name)).ReturnsAsync((ResourceEntity?)null);
            _repoMock.Setup(r => r.CreateResource(command)).ReturnsAsync(entity);

            var result = await _service.CreateResource(command);

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
            Assert.Equal(entity.Description, result.Description);
            Assert.Equal(entity.Region, result.Region);
            Assert.Equal(entity.Category, result.Category);
            Assert.Equal(entity.Group, result.Group);
        }

        [Fact]
        public async Task CreateResource_WhenResourceAlreadyExists_ThrowsInvalidOperationException()
        {
            var command = MakeCommand();
            _repoMock.Setup(r => r.GetResource(command.Name)).ReturnsAsync(MakeEntity());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateResource(command));
        }

        // ── GetResource ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetResource_WhenExists_ReturnsMappedResource()
        {
            var entity = MakeEntity();
            _repoMock.Setup(r => r.GetResource("Ferrite")).ReturnsAsync(entity);

            var result = await _service.GetResource("Ferrite");

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
        }

        [Fact]
        public async Task GetResource_WhenNotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetResource(It.IsAny<string>())).ReturnsAsync((ResourceEntity?)null);

            var result = await _service.GetResource("Missing");

            Assert.Null(result);
        }

        // ── GetAllResources ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllResources_ReturnsMappedList()
        {
            var entities = new List<ResourceEntity>
            {
                MakeEntity("Ferrite", "A common resource."),
                MakeEntity("Nano Spores", "Another common resource.")
            };
            _repoMock.Setup(r => r.GetAllResources()).ReturnsAsync(entities);

            var result = await _service.GetAllResources();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllResources_WhenEmpty_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllResources()).ReturnsAsync(new List<ResourceEntity>());

            var result = await _service.GetAllResources();

            Assert.Empty(result);
        }

        // ── DeleteResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteResource_WhenExists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteResource("Ferrite")).ReturnsAsync(true);

            var result = await _service.DeleteResource("Ferrite");

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteResource_WhenNotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.DeleteResource(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _service.DeleteResource("Missing");

            Assert.False(result);
        }

        // ── UpdateResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateResource_WhenExists_ReturnsMappedResource()
        {
            var command = MakeCommand("Ferrite", "Updated description.", "Plains of Eidolon", "Gems", "Rare");
            var updated = MakeEntity("Ferrite", "Updated description.", "Plains of Eidolon", "Gems", "Rare");

            _repoMock.Setup(r => r.GetResource(command.Name)).ReturnsAsync(MakeEntity());
            _repoMock.Setup(r => r.UpdateResource(command)).ReturnsAsync(updated);

            var result = await _service.UpdateResource(command);

            Assert.NotNull(result);
            Assert.Equal("Updated description.", result.Description);
            Assert.Equal("Plains of Eidolon", result.Region);
            Assert.Equal("Gems", result.Category);
            Assert.Equal("Rare", result.Group);
        }

        [Fact]
        public async Task UpdateResource_WhenNotFound_ThrowsInvalidOperationException()
        {
            _repoMock.Setup(r => r.GetResource(It.IsAny<string>())).ReturnsAsync((ResourceEntity?)null);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateResource(MakeCommand()));
        }
    }
}
