using GroupRequests.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GroupRequests.Database
{
    public class GroupRequestsDbContext : DbContext
    {
        public virtual DbSet<GroupRequestEntity> GroupRequests { get; set; }

        public GroupRequestsDbContext()
        {
        }

        public GroupRequestsDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
