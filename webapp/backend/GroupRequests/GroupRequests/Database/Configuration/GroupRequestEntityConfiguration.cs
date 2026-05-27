
using GroupRequests.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GroupRequests.Database.Configuration
{
    public class GroupRequestEntityConfiguration : IEntityTypeConfiguration<GroupRequestEntity>
    {
        public void Configure(EntityTypeBuilder<GroupRequestEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .UseIdentityByDefaultColumn();
            builder.Property(x => x.UserId)
                .IsRequired();
            builder.Property(x => x.InGameName)
                .IsRequired();
            builder.Property(x => x.ItemName)
                .IsRequired();
            builder.HasIndex(x => new { x.UserId, x.ItemName })
                .IsUnique();
        }
    }
}
