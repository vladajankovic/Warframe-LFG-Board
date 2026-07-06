namespace UserNotification.Database.Entities
{
    public class UserNotificationEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ItemName { get; set; }
        public string InGameName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
