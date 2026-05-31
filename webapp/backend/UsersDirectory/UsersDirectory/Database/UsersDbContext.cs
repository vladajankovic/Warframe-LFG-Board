using Microsoft.EntityFrameworkCore;
using System.Reflection;
using UsersDirectory.Database.Entities;

namespace UsersDirectory.Database
{
    public class UsersDbContext : DbContext
    {
        public virtual DbSet<UserEntity> Users { get; set; }

        public UsersDbContext()
        {
        }

        public UsersDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("UsersDirectorySchema");
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
