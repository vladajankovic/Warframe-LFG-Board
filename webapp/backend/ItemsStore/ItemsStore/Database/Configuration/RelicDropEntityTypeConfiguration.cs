using ItemsStore.Database.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItemsStore.Database.Configuration
{
    public class RelicDropEntityTypeConfiguration : IEntityTypeConfiguration<RelicDropEntity>
    {
        public void Configure(EntityTypeBuilder<RelicDropEntity> builder)
        {
            builder.HasKey(x => new { x.RelicId, x.ItemName });

            builder.Property(x => x.ItemName)
                .IsRequired();
            builder.Property(x => x.Rarity)
                .IsRequired();

            builder.HasOne(x => x.Relic)
                .WithMany(x => x.Drops)
                .HasForeignKey(x => x.RelicId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
