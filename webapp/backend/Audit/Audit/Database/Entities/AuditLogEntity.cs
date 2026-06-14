namespace Audit.Database.Entities
{
    public class AuditLogEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime DateTime { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
    }
}
