using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(r => r.Key)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyRecords_Key");

        builder.Property(r => r.ResponsePayload)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .IsRequired();

        builder.HasIndex(r => r.ExpiresAtUtc)
            .HasDatabaseName("IX_IdempotencyRecords_ExpiresAtUtc");
    }
}
