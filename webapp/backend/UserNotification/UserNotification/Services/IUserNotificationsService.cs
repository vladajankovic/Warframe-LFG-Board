using UserNotification.Database.Entities;
using UserNotification.Models;

namespace UserNotification.Services
{
    public interface IUserNotificationsService
    {
        Task<UserNotificationEntity> Create(
            string groupRequestUserId,
            string itemName,
            string inGameName,
            DateTime createdAt);

        Task<List<GroupRequestNotification>> GetPendingForUser(string userId, bool hasActiveSubscription);
        Task<bool> Dismiss(string userId, string notificationId);
    }
}
