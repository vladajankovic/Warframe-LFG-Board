namespace Audit.Models
{
    public class AuditLog
    {
        public DateTime DateTime { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
    }
}
