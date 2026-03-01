using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Infrastructure.Persistence;
using Serilog;
using Serilog.Events;

namespace SADC.Order.Management.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that replaces SQL Server with InMemoryDatabase
/// and removes hosted services (outbox publisher) for isolated integration tests.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Reset the static Serilog logger BEFORE the host builds,
        // preventing "logger is already frozen" when Program.Main runs again.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateBootstrapLogger();

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core / DbContext registrations to completely
            // replace the SqlServer provider with InMemory
            var typesToRemove = new HashSet<Type>
            {
                typeof(DbContextOptions<OrderManagementDbContext>),
                typeof(DbContextOptions),
                typeof(OrderManagementDbContext),
                typeof(IOrderManagementDbContext),
            };

            var descriptorsToRemove = services
                .Where(d =>
                    typesToRemove.Contains(d.ServiceType) ||
                    d.ServiceType.FullName?.Contains("DbContext") == true)
                .ToList();
            foreach (var d in descriptorsToRemove)
                services.Remove(d);

            // Remove hosted services (outbox publisher) to avoid RabbitMQ dependency
            var hostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var svc in hostedServices)
                services.Remove(svc);

            // Register InMemory database with explicit options (avoids dual-provider)
            var dbOptions = new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            services.AddSingleton(dbOptions);

            services.AddScoped<OrderManagementDbContext>(sp =>
                new OrderManagementDbContext(sp.GetRequiredService<DbContextOptions<OrderManagementDbContext>>()));

            services.AddScoped<IOrderManagementDbContext>(sp =>
                sp.GetRequiredService<OrderManagementDbContext>());
        });
    }
}
