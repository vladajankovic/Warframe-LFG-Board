using AutoMapper;
using ItemsStore.Broker.RabbitMQ;
using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Repository;
using ItemsStore.Services;
using ItemsStore.Utility.Mappers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ItemsStore.Tests.Services
{
    public class RelicServiceTests
    {
        private readonly Mock<IRelicRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<RelicService>> _loggerMock;
        private readonly Mock<IRabbitMQPublisher> _brokerMock;
        private readonly RelicService _service;

        public RelicServiceTests()
        {
            _repoMock = new Mock<IRelicRepository>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = config.CreateMapper();
            _loggerMock = new Mock<ILogger<RelicService>>();
            _brokerMock = new Mock<IRabbitMQPublisher>();
            _service = new RelicService(_repoMock.Object, _mapper, _loggerMock.Object, _brokerMock.Object);
        }

        private static RelicEntity MakeEntity(string name = "Lith A1", RelicType type = RelicType.LITH, string code = "A1") =>
            new() { Id = 1, Name = name, RelicName = name.ToLower().Replace(" ", "_"), RelicType = type, RelicCode = code };

        private static AddRelicCommand MakeCommand(string name = "Lith A1", RelicType type = RelicType.LITH, string code = "A1") =>
            new() { Name = name, Type = type, Code = code };

        // ── CreateRelic ──────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateRelic_WhenRelicDoesNotExist_ReturnsCreatedRelic()
        {
            var command = MakeCommand();
            var entity = MakeEntity();

            _repoMock.Setup(r => r.GetRelic(command.Name)).ReturnsAsync((RelicEntity?)null);
            _repoMock.Setup(r => r.CreateRelic(command)).ReturnsAsync(entity);

            var result = await _service.CreateRelic(command);

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
            Assert.Equal("lith", result.RelicType);
        }

        [Fact]
        public async Task CreateRelic_WhenRelicAlreadyExists_ThrowsInvalidOperationException()
        {
            var command = MakeCommand();
            _repoMock.Setup(r => r.GetRelic(command.Name)).ReturnsAsync(MakeEntity());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateRelic(command));
        }

        // ── GetRelic ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelic_WhenExists_ReturnsMappedRelic()
        {
            var entity = MakeEntity();
            entity.Drops.Add(new RelicDropEntity
            {
                RelicId = entity.Id,
                ItemName = "Forma Blueprint",
                Rarity = "common"
            });
            _repoMock.Setup(r => r.GetRelic("Lith A1 Intact")).ReturnsAsync(entity);

            var result = await _service.GetRelic("Lith A1 Intact");

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
            var drop = Assert.Single(result.Drops);
            Assert.Equal("Forma Blueprint", drop.ItemName);
            Assert.Equal("common", drop.Rarity);
        }

        [Fact]
        public async Task GetRelic_WhenNotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetRelic(It.IsAny<string>())).ReturnsAsync((RelicEntity?)null);

            var result = await _service.GetRelic("Missing");

            Assert.Null(result);
        }

        // ── GetAllRelics ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllRelics_ReturnsMappedList()
        {
            var entities = new List<RelicEntity> { MakeEntity(), MakeEntity("Meso B1", RelicType.MESO, "B1") };
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync(entities);

            var result = await _service.GetAllRelics();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllRelics_WhenEmpty_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync(new List<RelicEntity>());

            var result = await _service.GetAllRelics();

            Assert.Empty(result);
        }

        // ── GetRelicsByType ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelicsByType_FiltersByType()
        {
            var entities = new List<RelicEntity>
            {
                MakeEntity("Lith A1", RelicType.LITH, "A1"),
                MakeEntity("Meso B1", RelicType.MESO, "B1"),
            };
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync(entities);

            var result = await _service.GetRelicsByType(RelicType.LITH);

            Assert.Single(result);
            Assert.Equal("lith", result[0].RelicType);
        }

        [Fact]
        public async Task GetRelicsByType_WhenNullReturnedFromRepo_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync((List<RelicEntity>?)null!);

            var result = await _service.GetRelicsByType(RelicType.LITH);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRelicsByType_WhenNoMatch_ReturnsEmptyList()
        {
            var entities = new List<RelicEntity> { MakeEntity("Meso B1", RelicType.MESO, "B1") };
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync(entities);

            var result = await _service.GetRelicsByType(RelicType.LITH);

            Assert.Empty(result);
        }

        // ── GetRelicsByCode ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelicsByCode_FiltersByCode()
        {
            var entities = new List<RelicEntity>
            {
                MakeEntity("Lith A1", RelicType.LITH, "A1"),
                MakeEntity("Meso B1", RelicType.MESO, "B1"),
            };
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync(entities);

            var result = await _service.GetRelicsByCode("A1");

            Assert.Single(result);
            Assert.Equal("A1", result[0].RelicCode);
        }

        [Fact]
        public async Task GetRelicsByCode_WhenNullReturnedFromRepo_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllRelics()).ReturnsAsync((List<RelicEntity>?)null!);

            var result = await _service.GetRelicsByCode("A1");

            Assert.Empty(result);
        }

        // ── DeleteRelic ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteRelic_WhenExists_ReturnsTrue()
        {
            _repoMock.Setup(r => r.DeleteRelic("Lith A1 Intact")).ReturnsAsync(true);

            var result = await _service.DeleteRelic("Lith A1 Intact");

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteRelic_WhenNotFound_ReturnsFalse()
        {
            _repoMock.Setup(r => r.DeleteRelic(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _service.DeleteRelic("Missing");

            Assert.False(result);
        }

        // ── UpdateRelic ───────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateRelic_WhenExists_ReturnsMappedRelic()
        {
            var command = MakeCommand("Neo A2", RelicType.NEO, "A2");
            var updated = MakeEntity("Neo A2", RelicType.NEO, "A2");

            _repoMock.Setup(r => r.GetRelic(command.Name)).ReturnsAsync(MakeEntity());
            _repoMock.Setup(r => r.UpdateRelic(command)).ReturnsAsync(updated);

            var result = await _service.UpdateRelic(command);

            Assert.NotNull(result);
            Assert.Equal("neo", result.RelicType);
        }

        [Fact]
        public async Task UpdateRelic_WhenNotFound_ThrowsInvalidOperationException()
        {
            var command = MakeCommand();
            _repoMock.Setup(r => r.GetRelic(It.IsAny<string>())).ReturnsAsync((RelicEntity?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.UpdateRelic(command));
        }
    }
}
