using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SADC.Order.Management.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations tooling.
/// Used by 'dotnet ef migrations add' when the host cannot be built (e.g. without Aspire).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderManagementDbContext>
{
    public OrderManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderManagementDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=SadcOrderManagement;Trusted_Connection=True;TrustServerCertificate=True;");
        return new OrderManagementDbContext(optionsBuilder.Options);
    }
}
