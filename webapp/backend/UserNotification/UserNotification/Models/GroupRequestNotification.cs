namespace UserNotification.Models
{
    public class GroupRequestNotification
    {
        public required string Id { get; set; }
        public required string GroupRequestUserId { get; set; }
        public required string ItemName { get; set; }
        public required string InGameName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
