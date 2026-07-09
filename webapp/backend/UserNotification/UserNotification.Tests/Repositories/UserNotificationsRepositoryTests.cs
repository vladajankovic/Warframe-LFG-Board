using Microsoft.EntityFrameworkCore;
using UserNotification.Database;
using UserNotification.Database.Entities;
using UserNotification.Repositories;
using Xunit;

namespace UserNotification.Tests.Repositories
{
    public class UserNotificationsRepositoryTests : IDisposable
    {
        private readonly NotificationsDbContext _context;
        private readonly UserNotificationsRepository _repository;

        public UserNotificationsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new NotificationsDbContext(options);
            _repository = new UserNotificationsRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task Create_PersistsNotification()
        {
            var createdAt = DateTime.UtcNow;

            var result = await _repository.Create("host-1", "Forma", "Host", createdAt);

            Assert.NotNull(result.Id);
            Assert.Equal("host-1", result.GroupRequestUserId);
            Assert.Equal("Forma", result.ItemName);
            Assert.Equal("Host", result.InGameName);
            Assert.Equal(createdAt, result.CreatedAt);
            Assert.Single(_context.Notifications);
        }

        [Fact]
        public async Task GetPendingForUser_ReturnsOnlyMatchingUndismissedRecentNotifications()
        {
            var recentMatching = MakeNotification("notification-1", "Forma", DateTime.UtcNow.AddMinutes(-1));
            var dismissed = MakeNotification("notification-2", "Forma", DateTime.UtcNow.AddMinutes(-1));
            var wrongItem = MakeNotification("notification-3", "Orokin Cell", DateTime.UtcNow.AddMinutes(-1));
            var expired = MakeNotification("notification-4", "Forma", DateTime.UtcNow.AddMinutes(-10));

            await _context.Notifications.AddRangeAsync(recentMatching, dismissed, wrongItem, expired);
            await _context.NotificationDismissals.AddAsync(new UserNotificationDismissalEntity
            {
                NotificationId = dismissed.Id,
                UserId = "user-1",
                DismissedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _repository.GetPendingForUser(
                "user-1",
                ["Forma"],
                DateTime.UtcNow.AddMinutes(-5));

            Assert.Single(result);
            Assert.Equal("notification-1", result[0].Id);
        }

        [Fact]
        public async Task Dismiss_WhenNotificationExists_AddsDismissal()
        {
            var notification = MakeNotification("notification-1", "Forma", DateTime.UtcNow);
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            var result = await _repository.Dismiss("user-1", "notification-1");

            Assert.True(result);
            var dismissal = Assert.Single(_context.NotificationDismissals);
            Assert.Equal("notification-1", dismissal.NotificationId);
            Assert.Equal("user-1", dismissal.UserId);
        }

        [Fact]
        public async Task Dismiss_WhenAlreadyDismissed_IsIdempotent()
        {
            var notification = MakeNotification("notification-1", "Forma", DateTime.UtcNow);
            await _context.Notifications.AddAsync(notification);
            await _context.NotificationDismissals.AddAsync(new UserNotificationDismissalEntity
            {
                NotificationId = notification.Id,
                UserId = "user-1",
                DismissedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var result = await _repository.Dismiss("user-1", "notification-1");

            Assert.True(result);
            Assert.Single(_context.NotificationDismissals);
        }

        [Fact]
        public async Task Dismiss_WhenNotificationDoesNotExist_ReturnsFalse()
        {
            var result = await _repository.Dismiss("user-1", "missing");

            Assert.False(result);
            Assert.Empty(_context.NotificationDismissals);
        }

        private static UserNotificationEntity MakeNotification(string id, string itemName, DateTime createdAt)
        {
            return new UserNotificationEntity
            {
                Id = id,
                GroupRequestUserId = "host-1",
                ItemName = itemName,
                InGameName = "Host",
                CreatedAt = createdAt
            };
        }
    }
}
