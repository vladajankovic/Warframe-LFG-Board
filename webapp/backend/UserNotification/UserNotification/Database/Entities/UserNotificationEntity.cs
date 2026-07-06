namespace UserNotification.Database.Entities
{
    public class UserNotificationEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string GroupRequestUserId { get; set; }
        public string ItemName { get; set; }
        public string InGameName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<UserNotificationDismissalEntity> DismissedUsers { get; set; } = new();
    }
}
