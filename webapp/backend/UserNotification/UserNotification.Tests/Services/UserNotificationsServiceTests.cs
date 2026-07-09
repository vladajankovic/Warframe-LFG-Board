using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using UserNotification.Database.Entities;
using UserNotification.Repositories;
using UserNotification.Services;
using Xunit;

namespace UserNotification.Tests.Services
{
    public class UserNotificationsServiceTests
    {
        private readonly Mock<IUserNotificationsRepository> _notificationsRepositoryMock;
        private readonly Mock<IUserFavoritesRepository> _favoritesRepositoryMock;
        private readonly Mock<ILogger<UserNotificationsService>> _loggerMock;
        private readonly IConfiguration _configuration;
        private readonly UserNotificationsService _service;

        public UserNotificationsServiceTests()
        {
            _notificationsRepositoryMock = new Mock<IUserNotificationsRepository>();
            _favoritesRepositoryMock = new Mock<IUserFavoritesRepository>();
            _loggerMock = new Mock<ILogger<UserNotificationsService>>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Notifications:CleanupAfterMinutes"] = "7"
                })
                .Build();

            _service = new UserNotificationsService(
                _notificationsRepositoryMock.Object,
                _favoritesRepositoryMock.Object,
                _configuration,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Create_DelegatesToRepository()
        {
            var createdAt = DateTime.UtcNow;
            var entity = MakeNotification("notification-1", "host-user", "Forma", "Host", createdAt);

            _notificationsRepositoryMock
                .Setup(r => r.Create("host-user", "Forma", "Host", createdAt))
                .ReturnsAsync(entity);

            var result = await _service.Create("host-user", "Forma", "Host", createdAt);

            Assert.Same(entity, result);
        }

        [Fact]
        public async Task GetPendingForUser_WhenSubscriptionInactive_RemovesFavoritesAndReturnsEmptyList()
        {
            _favoritesRepositoryMock
                .Setup(r => r.DeleteAllFromUser("user-1"))
                .ReturnsAsync(3);

            var result = await _service.GetPendingForUser("user-1", hasActiveSubscription: false);

            Assert.Empty(result);
            _favoritesRepositoryMock.Verify(r => r.DeleteAllFromUser("user-1"), Times.Once);
            _notificationsRepositoryMock.Verify(
                r => r.GetPendingForUser(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task GetPendingForUser_WhenSubscriptionActive_FetchesNotificationsForFavoriteItems()
        {
            var favoriteItems = new List<UserFavoriteEntity>
            {
                new() { UserId = "user-1", ItemName = "Forma" },
                new() { UserId = "user-1", ItemName = "Forma" },
                new() { UserId = "user-1", ItemName = "Orokin Cell" }
            };
            var notifications = new List<UserNotificationEntity>
            {
                MakeNotification("notification-1", "host-1", "Forma", "HostOne"),
                MakeNotification("notification-2", "host-2", "Orokin Cell", "HostTwo")
            };

            _favoritesRepositoryMock
                .Setup(r => r.GetAllFromUser("user-1"))
                .ReturnsAsync(favoriteItems);
            _notificationsRepositoryMock
                .Setup(r => r.GetPendingForUser(
                    "user-1",
                    It.Is<IReadOnlyCollection<string>>(items =>
                        items.Count == 2 &&
                        items.Contains("Forma") &&
                        items.Contains("Orokin Cell")),
                    It.Is<DateTime>(notBefore => notBefore <= DateTime.UtcNow && notBefore >= DateTime.UtcNow.AddMinutes(-8))))
                .ReturnsAsync(notifications);

            var result = await _service.GetPendingForUser("user-1", hasActiveSubscription: true);

            Assert.Equal(2, result.Count);
            Assert.Equal("notification-1", result[0].Id);
            Assert.Equal("host-1", result[0].GroupRequestUserId);
            Assert.Equal("Forma", result[0].ItemName);
            Assert.Equal("HostOne", result[0].InGameName);
            _favoritesRepositoryMock.Verify(r => r.DeleteAllFromUser(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Dismiss_DelegatesToRepository()
        {
            _notificationsRepositoryMock
                .Setup(r => r.Dismiss("user-1", "notification-1"))
                .ReturnsAsync(true);

            var result = await _service.Dismiss("user-1", "notification-1");

            Assert.True(result);
        }

        private static UserNotificationEntity MakeNotification(
            string id,
            string groupRequestUserId,
            string itemName,
            string inGameName,
            DateTime? createdAt = null)
        {
            return new UserNotificationEntity
            {
                Id = id,
                GroupRequestUserId = groupRequestUserId,
                ItemName = itemName,
                InGameName = inGameName,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }
    }
}
