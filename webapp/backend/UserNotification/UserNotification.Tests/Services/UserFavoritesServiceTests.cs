using Microsoft.Extensions.Logging;
using Moq;
using UserNotification.Broker.Events;
using UserNotification.Broker.RabbitMQ;
using UserNotification.Commands;
using UserNotification.Database.Entities;
using UserNotification.Repositories;
using UserNotification.Services;
using Xunit;

namespace UserNotification.Tests.Services
{
    public class UserFavoritesServiceTests
    {
        private readonly Mock<IUserFavoritesRepository> _repositoryMock;
        private readonly Mock<IRabbitMQPublisher> _publisherMock;
        private readonly Mock<ILogger<UserFavoritesService>> _loggerMock;
        private readonly UserFavoritesService _service;

        public UserFavoritesServiceTests()
        {
            _repositoryMock = new Mock<IUserFavoritesRepository>();
            _publisherMock = new Mock<IRabbitMQPublisher>();
            _loggerMock = new Mock<ILogger<UserFavoritesService>>();
            _service = new UserFavoritesService(
                _repositoryMock.Object,
                _publisherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Create_WhenFavoriteAlreadyExists_ReturnsExistingFavoriteAndDoesNotPublishAudit()
        {
            var command = MakeCommand();
            var existing = MakeFavorite(command.UserId, command.ItemName);
            _repositoryMock
                .Setup(r => r.Get(command.UserId, command.ItemName))
                .ReturnsAsync(existing);

            var result = await _service.Create(command);

            Assert.Same(existing, result);
            _repositoryMock.Verify(r => r.Create(It.IsAny<AddOrRemoveUserFavoritesCommand>()), Times.Never);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task Create_WhenFavoriteDoesNotExist_CreatesFavoriteAndPublishesAudit()
        {
            var command = MakeCommand();
            var created = MakeFavorite(command.UserId, command.ItemName);
            _repositoryMock
                .Setup(r => r.Get(command.UserId, command.ItemName))
                .ReturnsAsync((UserFavoriteEntity?)null!);
            _repositoryMock
                .Setup(r => r.Create(command))
                .ReturnsAsync(created);

            var result = await _service.Create(command);

            Assert.Same(created, result);
            _publisherMock.Verify(p => p.PublishAsync("audit", It.IsAny<AuditEventMessage>()), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenRepositoryDeletesFavorite_PublishesAuditAndReturnsTrue()
        {
            var command = MakeCommand();
            _repositoryMock
                .Setup(r => r.Delete(command))
                .ReturnsAsync(true);

            var result = await _service.Delete(command);

            Assert.True(result);
            _publisherMock.Verify(p => p.PublishAsync("audit", It.IsAny<AuditEventMessage>()), Times.Once);
        }

        [Fact]
        public async Task Delete_WhenFavoriteDoesNotExist_DoesNotPublishAuditAndReturnsFalse()
        {
            var command = MakeCommand();
            _repositoryMock
                .Setup(r => r.Delete(command))
                .ReturnsAsync(false);

            var result = await _service.Delete(command);

            Assert.False(result);
            _publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task GetAll_GroupsFavoritesByUser()
        {
            _repositoryMock
                .Setup(r => r.GetAll())
                .ReturnsAsync([
                    MakeFavorite("user-1", "Forma"),
                    MakeFavorite("user-1", "Orokin Cell"),
                    MakeFavorite("user-2", "Ferrite")
                ]);

            var result = await _service.GetAll();

            Assert.Equal(2, result.Count);
            var userOne = Assert.Single(result, x => x.UserId == "user-1");
            Assert.Equal(["Forma", "Orokin Cell"], userOne.FavoriteItems);
        }

        [Fact]
        public async Task GetAllFromUser_ReturnsFavoriteItemsForUser()
        {
            _repositoryMock
                .Setup(r => r.GetAllFromUser("user-1"))
                .ReturnsAsync([
                    MakeFavorite("user-1", "Forma"),
                    MakeFavorite("user-1", "Orokin Cell")
                ]);

            var result = await _service.GetAllFromUser("user-1");

            Assert.Equal("user-1", result.UserId);
            Assert.Equal(["Forma", "Orokin Cell"], result.FavoriteItems);
        }

        private static AddOrRemoveUserFavoritesCommand MakeCommand()
        {
            return new AddOrRemoveUserFavoritesCommand
            {
                UserId = "user-1",
                ItemName = "Forma"
            };
        }

        private static UserFavoriteEntity MakeFavorite(string userId, string itemName)
        {
            return new UserFavoriteEntity
            {
                UserId = userId,
                ItemName = itemName
            };
        }
    }
}
