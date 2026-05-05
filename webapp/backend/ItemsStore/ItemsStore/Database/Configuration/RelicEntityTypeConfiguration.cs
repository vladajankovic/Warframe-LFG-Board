using ItemsStore.Database.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemsStore.Database.Configuration
{
    public class RelicEntityTypeConfiguration : IEntityTypeConfiguration<RelicEntity>
    {
        public void Configure(EntityTypeBuilder<RelicEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.RelicName)
                .IsUnique();
            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(30);
            builder.Property(x => x.RelicName)
                .IsRequired()
                .HasMaxLength(30);
            builder.Property(x => x.RelicType)
                .IsRequired();
            builder.Property(x => x.RelicCode)
                .IsRequired()
                .HasMaxLength(10);
        }
    }
}
