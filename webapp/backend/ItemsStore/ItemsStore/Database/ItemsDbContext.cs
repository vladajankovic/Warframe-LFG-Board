using ItemsStore.Database.Entity;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ItemsStore.Database
{
    public class ItemsDbContext : DbContext
    {
        public virtual DbSet<RelicEntity> Relics { get; set; }
        public virtual DbSet<ResourceEntity> Resources { get; set; }

        public ItemsDbContext()
        {
        }

        public ItemsDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
