using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Infrastructure.FxRates;
using SADC.Order.Management.Infrastructure.Messaging;
using SADC.Order.Management.Infrastructure.Persistence;

namespace SADC.Order.Management.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // EF Core — SQL Server
        services.AddDbContext<OrderManagementDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(OrderManagementDbContext).Assembly.FullName)));

        services.AddScoped<IOrderManagementDbContext>(sp =>
            sp.GetRequiredService<OrderManagementDbContext>());

        // RabbitMQ — test connectivity and fall back to NullMessagePublisher
        var rabbitSection = configuration.GetSection(RabbitMqSettings.SectionName);
        services.Configure<RabbitMqSettings>(rabbitSection);

        if (IsRabbitMqReachable(rabbitSection))
        {
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
            services.AddHostedService<OutboxPublisherService>();
        }
        else
        {
            services.AddSingleton<IMessagePublisher, NullMessagePublisher>();
            // No outbox publisher — messages stay queued until RabbitMQ is available
        }

        // FX rates
        services.AddMemoryCache();
        services.AddSingleton<IFxRateProvider, MockFxRateProvider>();

        return services;
    }

    private static bool IsRabbitMqReachable(IConfigurationSection section)
    {
        var host = section["HostName"] ?? "localhost";
        var port = int.TryParse(section["Port"], out var p) ? p : 5672;

        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            tcp.Connect(host, port);
            return true;
        }
        catch
        {
            Console.WriteLine($"[Infrastructure] RabbitMQ not reachable at {host}:{port} — using NullMessagePublisher");
            return false;
        }
    }
}
