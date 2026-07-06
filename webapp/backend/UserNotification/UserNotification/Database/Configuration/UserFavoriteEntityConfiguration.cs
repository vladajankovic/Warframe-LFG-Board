using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserNotification.Database.Entities;

namespace UserNotification.Database.Configuration
{
    public class UserFavoriteEntityConfiguration : IEntityTypeConfiguration<UserFavoriteEntity>
    {
        public void Configure(EntityTypeBuilder<UserFavoriteEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.UserId, x.ItemName })
                .IsUnique();
            builder.Property(x => x.UserId)
                .IsRequired();
            builder.Property(x => x.ItemName)
                .IsRequired();
        }
    }
}
