using AutoMapper;
using GroupRequests.Commands;
using GroupRequests.Database.Entities;
using GroupRequests.Models;
using GroupRequests.Repositories;
using GroupRequests.Services;
using GroupRequests.Utility.Mappers;
using Moq;
using Xunit;

namespace GroupRequests.Tests.Services
{
    public class GroupRequestsServiceTests
    {
        private readonly Mock<IGroupRequestsRepository> _repoMock;
        private readonly IMapper _mapper;
        private readonly GroupRequestsService _service;

        public GroupRequestsServiceTests()
        {
            _repoMock = new Mock<IGroupRequestsRepository>();
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            _mapper = config.CreateMapper();
            _service = new GroupRequestsService(_repoMock.Object, _mapper);
        }

        private static GroupRequestEntity MakeEntity(string userId = "user1", string inGameName = "PlayerOne", string itemName = "Forma Blueprint") =>
            new() { Id = 1, UserId = userId, InGameName = inGameName, ItemName = itemName };

        private static AddGroupRequestCommand MakeAddCommand(string userId = "user1", string inGameName = "PlayerOne", string itemName = "Forma Blueprint") =>
            new() { UserId = userId, InGameName = inGameName, ItemName = itemName };

        private static DeleteGroupRequestCommand MakeDeleteCommand(string userId = "user1", string itemName = "Forma Blueprint") =>
            new() { UserId = userId, ItemName = itemName };

        // ── CreateGroupRequest ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateGroupRequest_WhenUserHasNoExistingRequests_ReturnsCreatedDTO()
        {
            var command = MakeAddCommand();
            var entity = MakeEntity();

            _repoMock.Setup(r => r.GetGroupRequestsByUserId(command.UserId))
                     .ReturnsAsync(new List<GroupRequestEntity>());
            _repoMock.Setup(r => r.CreateGroupRequest(command))
                     .ReturnsAsync(entity);

            var result = await _service.CreateGroupRequest(command);

            Assert.NotNull(result);
            Assert.Equal(entity.UserId, result.UserId);
            Assert.Equal(entity.InGameName, result.InGameName);
            Assert.Equal(entity.ItemName, result.ItemName);
        }

        [Fact]
        public async Task CreateGroupRequest_WhenUserHasNullExistingRequests_ReturnsCreatedDTO()
        {
            var command = MakeAddCommand();
            var entity = MakeEntity();

            _repoMock.Setup(r => r.GetGroupRequestsByUserId(command.UserId))
                     .ReturnsAsync((List<GroupRequestEntity>?)null!);
            _repoMock.Setup(r => r.CreateGroupRequest(command))
                     .ReturnsAsync(entity);

            var result = await _service.CreateGroupRequest(command);

            Assert.NotNull(result);
            Assert.Equal(entity.UserId, result.UserId);
        }

        [Fact]
        public async Task CreateGroupRequest_WhenUserAlreadyHasRequest_ThrowsInvalidOperationException()
        {
            var command = MakeAddCommand();

            _repoMock.Setup(r => r.GetGroupRequestsByUserId(command.UserId))
                     .ReturnsAsync(new List<GroupRequestEntity> { MakeEntity() });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateGroupRequest(command));
        }

        [Fact]
        public async Task CreateGroupRequest_WhenUserAlreadyHasRequest_DoesNotCallCreateOnRepository()
        {
            var command = MakeAddCommand();

            _repoMock.Setup(r => r.GetGroupRequestsByUserId(command.UserId))
                     .ReturnsAsync(new List<GroupRequestEntity> { MakeEntity() });

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateGroupRequest(command));

            _repoMock.Verify(r => r.CreateGroupRequest(It.IsAny<AddGroupRequestCommand>()), Times.Never);
        }

        // ── DeleteGroupRequest ────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteGroupRequest_WhenExists_ReturnsTrue()
        {
            var command = MakeDeleteCommand();
            _repoMock.Setup(r => r.DeleteGroupRequest(command)).ReturnsAsync(true);

            var result = await _service.DeleteGroupRequest(command);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteGroupRequest_WhenNotFound_ReturnsFalse()
        {
            var command = MakeDeleteCommand();
            _repoMock.Setup(r => r.DeleteGroupRequest(command)).ReturnsAsync(false);

            var result = await _service.DeleteGroupRequest(command);

            Assert.False(result);
        }

        // ── GetAllGroupRequests ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAllGroupRequests_ReturnsMappedList()
        {
            var entities = new List<GroupRequestEntity>
            {
                MakeEntity("user1", "PlayerOne", "Forma Blueprint"),
                MakeEntity("user2", "PlayerTwo", "Orokin Cell"),
            };
            _repoMock.Setup(r => r.GetAllGroupRequests()).ReturnsAsync(entities);

            var result = await _service.GetAllGroupRequests();

            Assert.Equal(2, result.Count);
            Assert.Equal("user1", result[0].UserId);
            Assert.Equal("user2", result[1].UserId);
        }

        [Fact]
        public async Task GetAllGroupRequests_WhenEmpty_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetAllGroupRequests()).ReturnsAsync(new List<GroupRequestEntity>());

            var result = await _service.GetAllGroupRequests();

            Assert.Empty(result);
        }

        // ── GetGroupRequestsByItemName ─────────────────────────────────────────────

        [Fact]
        public async Task GetGroupRequestsByItemName_ReturnsMappedList()
        {
            var itemName = "Forma Blueprint";
            var entities = new List<GroupRequestEntity>
            {
                MakeEntity("user1", "PlayerOne", itemName),
                MakeEntity("user2", "PlayerTwo", itemName),
            };
            _repoMock.Setup(r => r.GetGroupRequestsByItemName(itemName)).ReturnsAsync(entities);

            var result = await _service.GetGroupRequestsByItemName(itemName);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(itemName, r.ItemName));
        }

        [Fact]
        public async Task GetGroupRequestsByItemName_WhenNoneFound_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetGroupRequestsByItemName(It.IsAny<string>()))
                     .ReturnsAsync(new List<GroupRequestEntity>());

            var result = await _service.GetGroupRequestsByItemName("Unknown Item");

            Assert.Empty(result);
        }

        // ── GetGroupRequestsByUserId ───────────────────────────────────────────────

        [Fact]
        public async Task GetGroupRequestsByUserId_ReturnsMappedList()
        {
            var userId = "user1";
            var entities = new List<GroupRequestEntity>
            {
                MakeEntity(userId, "PlayerOne", "Forma Blueprint"),
                MakeEntity(userId, "PlayerOne", "Orokin Cell"),
            };
            _repoMock.Setup(r => r.GetGroupRequestsByUserId(userId)).ReturnsAsync(entities);

            var result = await _service.GetGroupRequestsByUserId(userId);

            Assert.Equal(2, result.Count);
            Assert.All(result, r => Assert.Equal(userId, r.UserId));
        }

        [Fact]
        public async Task GetGroupRequestsByUserId_WhenNoneFound_ReturnsEmptyList()
        {
            _repoMock.Setup(r => r.GetGroupRequestsByUserId(It.IsAny<string>()))
                     .ReturnsAsync(new List<GroupRequestEntity>());

            var result = await _service.GetGroupRequestsByUserId("unknown");

            Assert.Empty(result);
        }

        // ── GetGroupRequest ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetGroupRequest_WhenExists_ReturnsMappedDTO()
        {
            var entity = MakeEntity();
            _repoMock.Setup(r => r.GetGroupRequest(entity.UserId, entity.ItemName))
                     .ReturnsAsync(entity);

            var result = await _service.GetGroupRequest(entity.UserId, entity.ItemName);

            Assert.NotNull(result);
            Assert.Equal(entity.UserId, result.UserId);
            Assert.Equal(entity.InGameName, result.InGameName);
            Assert.Equal(entity.ItemName, result.ItemName);
        }

        [Fact]
        public async Task GetGroupRequest_WhenNotFound_ReturnsNull()
        {
            _repoMock.Setup(r => r.GetGroupRequest(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync((GroupRequestEntity?)null!);

            var result = await _service.GetGroupRequest("nobody", "nothing");

            Assert.Null(result);
        }
    }
}
