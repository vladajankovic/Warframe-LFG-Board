using AutoMapper;
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
        private readonly IMapper _mapper;

        public UserNotificationsService(
            IUserNotificationsRepository notificationsRepository,
            IUserFavoritesRepository favoritesRepository,
            IConfiguration configuration,
            ILogger<UserNotificationsService> logger,
            IMapper mapper)
        {
            _notificationsRepository = notificationsRepository;
            _favoritesRepository = favoritesRepository;
            _configuration = configuration;
            _logger = logger;
            _mapper = mapper;
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

            return _mapper.Map<List<GroupRequestNotification>>(notifications);
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
    }
}
