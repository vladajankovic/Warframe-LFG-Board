namespace ItemsStore.Broker.RabbitMQ
{
    public class AuditEventMessage
    {
        public DateTime DateTime { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }

        public AuditEventMessage(string action, string user, string details)
        {
            DateTime = DateTime.UtcNow;
            Service = "ItemsStore";
            Action = action;
            User = user;
            Details = details;
        }
    }
}
