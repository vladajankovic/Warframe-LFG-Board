using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UsersDirectory.Database.Entities;

namespace UsersDirectory.Database.Configuration
{
    public class UserEntityTypeConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.Email)
                .IsUnique();
            builder.Property(x => x.Id)
                .IsRequired();
            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(254);
            builder.Property(x => x.Password)
                .IsRequired();
            builder.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");
            builder.Property(x => x.InGameName)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            builder.Property(x => x.LastUpdatedAt)
                .IsRequired();
            builder.Property(x => x.VerificationCode)
                .IsRequired()
                .HasMaxLength(64);
            builder.Property(x => x.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}
