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
        }
    }
}
