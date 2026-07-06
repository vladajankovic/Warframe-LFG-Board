using Microsoft.EntityFrameworkCore;
using System.Reflection;
using UserNotification.Database.Entities;

namespace UserNotification.Database
{
    public class NotificationsDbContext : DbContext
    {
        public virtual DbSet<UserFavoriteEntity> Favorites { get; set; }
        public virtual DbSet<UserNotificationEntity> Notifications { get; set; }
        public virtual DbSet<UserNotificationDismissalEntity> NotificationDismissals { get; set; }
        public NotificationsDbContext() : base() 
        {
        }

        public NotificationsDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("UserNotificationsSchema");
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
