namespace UserNotification.Broker.Events
{
    public class GroupRequestCreatedEvent
    {
        public string UserId { get; set; }
        public string ItemName { get; set; }
        public string InGameName { get; set; }
        public DateTime CreatedAt { get; set; }

        public GroupRequestCreatedEvent()
        {
            CreatedAt = DateTime.UtcNow;
        }

        public GroupRequestCreatedEvent(string userId, string itemName, string inGameName)
        {
            UserId = userId;
            ItemName = itemName;
            InGameName = inGameName;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
