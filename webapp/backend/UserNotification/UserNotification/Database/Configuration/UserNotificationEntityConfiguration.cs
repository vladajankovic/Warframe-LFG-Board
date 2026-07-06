using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserNotification.Database.Entities;

namespace UserNotification.Database.Configuration
{
    public class UserNotificationEntityConfiguration : IEntityTypeConfiguration<UserNotificationEntity>
    {
        public void Configure(EntityTypeBuilder<UserNotificationEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.ItemName, x.CreatedAt });
            builder.Property(x => x.GroupRequestUserId)
                .IsRequired();
            builder.Property(x => x.ItemName)
                .IsRequired();
            builder.Property(x => x.InGameName)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .IsRequired();
        }
    }
}
