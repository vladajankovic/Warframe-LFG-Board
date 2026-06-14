using Audit.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Audit.Database
{
    public class AuditDbContext : DbContext
    {
        public virtual DbSet<AuditLogEntity> AuditLog { get; set; }
        public AuditDbContext()
        {
        }

        public AuditDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("AuditLogsSchema");
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
