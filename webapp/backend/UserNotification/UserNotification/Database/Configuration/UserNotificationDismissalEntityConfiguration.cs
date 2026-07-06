using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserNotification.Database.Entities;

namespace UserNotification.Database.Configuration
{
    public class UserNotificationDismissalEntityConfiguration : IEntityTypeConfiguration<UserNotificationDismissalEntity>
    {
        public void Configure(EntityTypeBuilder<UserNotificationDismissalEntity> builder)
        {
            builder.HasKey(x => new { x.NotificationId, x.UserId });
            builder.HasIndex(x => x.UserId);
            builder.Property(x => x.DismissedAt)
                .IsRequired();

            builder.HasOne(x => x.Notification)
                .WithMany(x => x.DismissedUsers)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
