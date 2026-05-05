using ItemsStore.Commands;
using ItemsStore.Controllers;
using ItemsStore.Database.Entity;
using ItemsStore.Models;
using ItemsStore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ItemsStore.Tests.Controllers
{
    public class RelicsControllerTests
    {
        private readonly Mock<IRelicService> _serviceMock;
        private readonly RelicsController _controller;

        public RelicsControllerTests()
        {
            _serviceMock = new Mock<IRelicService>();
            var loggerMock = new Mock<ILogger<RelicsController>>();
            _controller = new RelicsController(_serviceMock.Object, loggerMock.Object);
        }

        private static Relic MakeRelic(string name = "Lith A1 Intact", string type = "lith", string code = "A1") =>
            new() { Id = 1, Name = name, RelicType = type, RelicCode = code };

        private static AddRelicCommand MakeCommand(string name = "Lith A1 Intact", RelicType type = RelicType.LITH, string code = "A1") =>
            new() { Name = name, Type = type, Code = code };

        // ── GET all ──────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllRelics_Returns200WithList()
        {
            var relics = new List<Relic> { MakeRelic() };
            _serviceMock.Setup(s => s.GetAllRelics()).ReturnsAsync(relics);

            var result = await _controller.GetAllRelics();

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(relics, ok.Value);
        }

        // ── GET single ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelic_WhenFound_Returns200()
        {
            var relic = MakeRelic();
            _serviceMock.Setup(s => s.GetRelic("lith_a1_intact")).ReturnsAsync(relic);

            var result = await _controller.GetRelic("lith_a1_intact");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(relic, ok.Value);
        }

        [Fact]
        public async Task GetRelic_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.GetRelic(It.IsAny<string>())).ReturnsAsync((Relic?)null);

            var result = await _controller.GetRelic("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── GET by type ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelicsByType_WhenFound_Returns200()
        {
            var relics = new List<Relic> { MakeRelic() };
            _serviceMock.Setup(s => s.GetRelicsByType(RelicType.LITH)).ReturnsAsync(relics);

            var result = await _controller.GetRelicsByType("lith");

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(relics, ok.Value);
        }

        [Fact]
        public async Task GetRelicsByType_WhenEmpty_Returns404()
        {
            _serviceMock.Setup(s => s.GetRelicsByType(It.IsAny<RelicType>())).ReturnsAsync(new List<Relic>());

            var result = await _controller.GetRelicsByType("lith");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetRelicsByType_WithUnknownType_Returns404()
        {
            _serviceMock.Setup(s => s.GetRelicsByType(RelicType.UNKNOWN)).ReturnsAsync(new List<Relic>());

            var result = await _controller.GetRelicsByType("invalid_type");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── GET by code ───────────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelicsByCode_WhenFound_Returns200()
        {
            var relics = new List<Relic> { MakeRelic() };
            _serviceMock.Setup(s => s.GetRelicsByCode("A1")).ReturnsAsync(relics);

            var result = await _controller.GetRelicsByCode("a1");   // controller calls ToUpper()

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(relics, ok.Value);
        }

        [Fact]
        public async Task GetRelicsByCode_WhenEmpty_Returns404()
        {
            _serviceMock.Setup(s => s.GetRelicsByCode(It.IsAny<string>())).ReturnsAsync(new List<Relic>());

            var result = await _controller.GetRelicsByCode("zz");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── POST ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task AddNewRelic_Returns201WithCreatedRelic()
        {
            var command = MakeCommand();
            var created = MakeRelic();
            _serviceMock.Setup(s => s.CreateRelic(command)).ReturnsAsync(created);

            var result = await _controller.AddNewRelic(command);

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(201, status.StatusCode);
            Assert.Equal(created, status.Value);
        }

        // ── DELETE ────────────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteRelic_WhenDeleted_Returns204()
        {
            _serviceMock.Setup(s => s.DeleteRelic("lith_a1_intact")).ReturnsAsync(true);

            var result = await _controller.DeleteRelic("lith_a1_intact");

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteRelic_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.DeleteRelic(It.IsAny<string>())).ReturnsAsync(false);

            var result = await _controller.DeleteRelic("missing");

            Assert.IsType<NotFoundResult>(result);
        }

        // ── PUT ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateRelic_WhenUpdated_Returns200()
        {
            var command = MakeCommand();
            var updated = MakeRelic();
            _serviceMock.Setup(s => s.UpdateRelic(command)).ReturnsAsync(updated);

            var result = await _controller.UpdateRelic(command);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(updated, ok.Value);
        }

        [Fact]
        public async Task UpdateRelic_WhenNotFound_Returns404()
        {
            _serviceMock.Setup(s => s.UpdateRelic(It.IsAny<AddRelicCommand>()))
                .ThrowsAsync(new InvalidOperationException("Not found"));

            var result = await _controller.UpdateRelic(MakeCommand());

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
