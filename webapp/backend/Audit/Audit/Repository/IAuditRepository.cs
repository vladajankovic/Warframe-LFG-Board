using Audit.Commands;
using Audit.Database.Entities;
using Audit.Models;

namespace Audit.Repository
{
    public interface IAuditRepository
    {
        public Task<AuditLogEntity> CreateAuditLog(CreateAuditLogCommand command);
        public Task<List<AuditLog>> Get(DateTime? from, DateTime? to, string service, string action, string user);
    }
}
