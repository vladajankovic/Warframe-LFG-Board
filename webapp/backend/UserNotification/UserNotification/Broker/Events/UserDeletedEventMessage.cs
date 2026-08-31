namespace UserNotification.Broker.Events
{
    public class UserDeletedEventMessage
    {
        public string UserId { get; set; }

        public UserDeletedEventMessage(string userId)
        {
            UserId = userId;
        }
    }
}
