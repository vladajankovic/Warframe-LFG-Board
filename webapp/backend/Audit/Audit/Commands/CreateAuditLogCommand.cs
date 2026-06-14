namespace Audit.Commands
{
    public class CreateAuditLogCommand
    {
        public DateTime DateTime { get; set; }
        public string Service { get; set; }
        public string Action { get; set; }
        public string User { get; set; }
        public string Details { get; set; }
    }
}
