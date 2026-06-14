using Audit.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Database.Configuration
{
    public class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
    {
        public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .IsRequired();
            builder.Property(x => x.DateTime)
                .IsRequired();
            builder.Property(x => x.Service)
                .IsRequired()
                .HasMaxLength(64);
            builder.Property(x => x.Action)
                .IsRequired()
                .HasMaxLength(128);
            builder.Property(x => x.User)
                .IsRequired()
                .HasMaxLength(254);
            builder.Property(x => x.Details)
                .IsRequired();
        }
    }
}
