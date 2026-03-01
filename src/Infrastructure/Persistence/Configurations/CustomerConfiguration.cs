using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SADC.Order.Management.Domain.Entities;

namespace SADC.Order.Management.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(c => c.CountryCode)
            .IsRequired()
            .HasMaxLength(2)
            .IsFixedLength();

        builder.Property(c => c.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(c => c.Email)
            .IsUnique()
            .HasDatabaseName("IX_Customers_Email");

        builder.HasIndex(c => c.CountryCode)
            .HasDatabaseName("IX_Customers_CountryCode");
    }
}
