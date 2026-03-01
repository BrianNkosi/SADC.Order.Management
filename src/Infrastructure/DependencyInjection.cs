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

        // RabbitMQ — supports both Aspire connection string (amqp:// URI) and individual settings
        var rabbitSection = configuration.GetSection(RabbitMqSettings.SectionName);
        services.Configure<RabbitMqSettings>(rabbitSection);

        // Aspire injects RabbitMQ via ConnectionStrings:messaging
        var rabbitConnectionString = configuration.GetConnectionString("messaging");

        if (!string.IsNullOrEmpty(rabbitConnectionString) || IsRabbitMqReachable(rabbitSection, rabbitConnectionString))
        {
            if (!string.IsNullOrEmpty(rabbitConnectionString))
            {
                // Override RabbitMqSettings from the Aspire connection string
                services.PostConfigure<RabbitMqSettings>(settings =>
                {
                    var uri = new Uri(rabbitConnectionString);
                    settings.HostName = uri.Host;
                    settings.Port = uri.Port > 0 ? uri.Port : 5672;
                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        var parts = uri.UserInfo.Split(':');
                        settings.UserName = Uri.UnescapeDataString(parts[0]);
                        settings.Password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "guest";
                    }
                    if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                    {
                        settings.VirtualHost = Uri.UnescapeDataString(uri.AbsolutePath);
                    }
                });
            }

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

    private static bool IsRabbitMqReachable(IConfigurationSection section, string? connectionString)
    {
        string host;
        int port;

        if (!string.IsNullOrEmpty(connectionString))
        {
            try
            {
                var uri = new Uri(connectionString);
                host = uri.Host;
                port = uri.Port > 0 ? uri.Port : 5672;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            host = section["HostName"] ?? "localhost";
            port = int.TryParse(section["Port"], out var p) ? p : 5672;
        }

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
