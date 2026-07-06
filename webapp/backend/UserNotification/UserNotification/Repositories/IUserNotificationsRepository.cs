using UserNotification.Database.Entities;

namespace UserNotification.Repositories
{
    public interface IUserNotificationsRepository
    {
        Task<UserNotificationEntity> Create(string groupRequestUserId, string itemName, string inGameName, DateTime createdAt);
        Task<List<UserNotificationEntity>> GetPendingForUser(string userId, IReadOnlyCollection<string> itemNames, DateTime notBefore);
        Task<bool> Dismiss(string userId, string notificationId);
        Task<int> DeleteOlderThan(DateTime cutoff, CancellationToken cancellationToken = default);
    }
}
