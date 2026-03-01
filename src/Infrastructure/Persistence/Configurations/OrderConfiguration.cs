using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SADC.Order.Management.Domain.Entities;
using SADC.Order.Management.Domain.Enums;
using OrderEntity = SADC.Order.Management.Domain.Entities.Order;

namespace SADC.Order.Management.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.CurrencyCode)
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(o => o.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.RowVersion)
            .IsRowVersion();

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        // FK to Customer — Restrict delete for audit trail
        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite index for common query patterns
        builder.HasIndex(o => new { o.CustomerId, o.Status, o.CreatedAtUtc })
            .HasDatabaseName("IX_Orders_CustomerId_Status_CreatedAt");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");

        builder.HasIndex(o => o.CreatedAtUtc)
            .HasDatabaseName("IX_Orders_CreatedAt");
    }
}
