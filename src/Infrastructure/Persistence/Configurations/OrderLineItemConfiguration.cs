using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Infrastructure.Persistence.Configurations;

public class OrderLineItemConfiguration : IEntityTypeConfiguration<OrderLineItem>
{
    public void Configure(EntityTypeBuilder<OrderLineItem> builder)
    {
        builder.ToTable("OrderLineItems");

        builder.HasKey(li => li.Id);
        builder.Property(li => li.Id).ValueGeneratedNever();

        builder.Property(li => li.ProductSku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(li => li.Quantity)
            .IsRequired();

        builder.Property(li => li.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        // FK to Order — Cascade delete (line items are owned by order)
        builder.HasOne(li => li.Order)
            .WithMany(o => o.LineItems)
            .HasForeignKey(li => li.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(li => li.OrderId)
            .HasDatabaseName("IX_OrderLineItems_OrderId");
    }
}
