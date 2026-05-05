using ItemsStore.Database.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemsStore.Database.Configuration
{
    public class ResourceEntityTypeConfiguration : IEntityTypeConfiguration<ResourceEntity>
    {
        public void Configure(EntityTypeBuilder<ResourceEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(128);
            builder.Property(x => x.Description)
                .HasMaxLength(512);
        }
    }
}
