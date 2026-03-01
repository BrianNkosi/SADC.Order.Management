using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.CreatedAtUtc)
            .IsRequired();

        builder.Property(m => m.Error)
            .HasMaxLength(2000);

        // Index for polling unprocessed messages
        builder.HasIndex(m => m.ProcessedAtUtc)
            .HasDatabaseName("IX_OutboxMessages_ProcessedAtUtc")
            .HasFilter("[ProcessedAtUtc] IS NULL");
    }
}
