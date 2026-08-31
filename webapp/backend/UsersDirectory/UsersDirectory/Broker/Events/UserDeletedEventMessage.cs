namespace UsersDirectory.Broker.RabbitMQ
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
