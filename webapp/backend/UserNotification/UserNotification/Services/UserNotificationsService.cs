using UserNotification.Database.Entities;
using UserNotification.Models;
using UserNotification.Repositories;

namespace UserNotification.Services
{
    public class UserNotificationsService : IUserNotificationsService
    {
        private const int DefaultCleanupAfterMinutes = 5;

        private readonly IUserNotificationsRepository _notificationsRepository;
        private readonly IUserFavoritesRepository _favoritesRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserNotificationsService> _logger;

        public UserNotificationsService(
            IUserNotificationsRepository notificationsRepository,
            IUserFavoritesRepository favoritesRepository,
            IConfiguration configuration,
            ILogger<UserNotificationsService> logger)
        {
            _notificationsRepository = notificationsRepository;
            _favoritesRepository = favoritesRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public Task<UserNotificationEntity> Create(
            string groupRequestUserId,
            string itemName,
            string inGameName,
            DateTime createdAt)
        {
            return _notificationsRepository.Create(groupRequestUserId, itemName, inGameName, createdAt);
        }

        public async Task<List<GroupRequestNotification>> GetPendingForUser(string userId, bool hasActiveSubscription)
        {
            if (!hasActiveSubscription)
            {
                var deletedFavoritesCount = await _favoritesRepository.DeleteAllFromUser(userId);
                _logger.LogInformation(
                    "Removed {FavoriteCount} favorites from inactive subscription user {UserId}",
                    deletedFavoritesCount,
                    userId);

                return [];
            }

            var favorites = await _favoritesRepository.GetAllFromUser(userId);
            var favoriteItemNames = favorites.Select(x => x.ItemName).Distinct().ToList();
            var notBefore = DateTime.UtcNow.AddMinutes(-GetCleanupAfterMinutes());
            var notifications = await _notificationsRepository.GetPendingForUser(userId, favoriteItemNames, notBefore);

            return notifications.Select(MapToModel).ToList();
        }

        public Task<bool> Dismiss(string userId, string notificationId)
        {
            return _notificationsRepository.Dismiss(userId, notificationId);
        }

        private int GetCleanupAfterMinutes()
        {
            var configuredValue =
                _configuration["CLEANUP_NOTIFICATIONS_AFTER"] ??
                _configuration["Notifications:CleanupAfterMinutes"];

            if (int.TryParse(configuredValue, out var cleanupAfterMinutes) && cleanupAfterMinutes > 0)
            {
                return cleanupAfterMinutes;
            }

            return DefaultCleanupAfterMinutes;
        }

        private static GroupRequestNotification MapToModel(UserNotificationEntity entity)
        {
            return new GroupRequestNotification
            {
                Id = entity.Id,
                GroupRequestUserId = entity.GroupRequestUserId,
                ItemName = entity.ItemName,
                InGameName = entity.InGameName,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
