namespace UserNotification.Database.Entities
{
    public class UserNotificationDismissalEntity
    {
        public string NotificationId { get; set; }
        public string UserId { get; set; }
        public DateTime DismissedAt { get; set; } = DateTime.UtcNow;
        public UserNotificationEntity Notification { get; set; }
    }
}
