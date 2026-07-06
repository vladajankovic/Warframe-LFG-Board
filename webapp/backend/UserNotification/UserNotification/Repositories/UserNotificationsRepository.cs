using Microsoft.EntityFrameworkCore;
using UserNotification.Database;
using UserNotification.Database.Entities;

namespace UserNotification.Repositories
{
    public class UserNotificationsRepository : IUserNotificationsRepository
    {
        private readonly NotificationsDbContext _dbContext;

        public UserNotificationsRepository(NotificationsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserNotificationEntity> Create(string groupRequestUserId, string itemName, string inGameName, DateTime createdAt)
        {
            var notification = new UserNotificationEntity
            {
                GroupRequestUserId = groupRequestUserId,
                ItemName = itemName,
                InGameName = inGameName,
                CreatedAt = createdAt
            };

            await _dbContext.Notifications.AddAsync(notification);
            await _dbContext.SaveChangesAsync();

            return notification;
        }

        public async Task<List<UserNotificationEntity>> GetPendingForUser(string userId, IReadOnlyCollection<string> itemNames, DateTime notBefore)
        {
            if (itemNames.Count == 0)
            {
                return [];
            }

            return await _dbContext.Notifications
                .Where(x =>
                    itemNames.Contains(x.ItemName) &&
                    x.CreatedAt >= notBefore &&
                    !x.DismissedUsers.Any(dismissal => dismissal.UserId.Equals(userId)))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> Dismiss(string userId, string notificationId)
        {
            var notificationExists = await _dbContext.Notifications.AnyAsync(x => x.Id.Equals(notificationId));
            if (!notificationExists)
            {
                return false;
            }

            var dismissalExists = await _dbContext.NotificationDismissals
                .AnyAsync(x => x.NotificationId.Equals(notificationId) && x.UserId.Equals(userId));

            if (dismissalExists)
            {
                return true;
            }

            await _dbContext.NotificationDismissals.AddAsync(new UserNotificationDismissalEntity
            {
                NotificationId = notificationId,
                UserId = userId,
                DismissedAt = DateTime.UtcNow
            });

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<int> DeleteOlderThan(DateTime cutoff, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Notifications
                .Where(x => x.CreatedAt < cutoff)
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
