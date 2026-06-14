namespace Audit.Broker.RabbitMQ
{
    public class AuditEventMessage
    {
        public DateTime DateTime { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }

        public AuditEventMessage() { }

        public AuditEventMessage(DateTime dateTime, string service, string action, string user, string details)
        {
            DateTime = dateTime;
            Service = service;
            Action = action;
            User = user;
            Details = details;
        }
    }
}
