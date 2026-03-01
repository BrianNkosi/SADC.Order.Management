using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SADC.Order.Management.Infrastructure;
using SADC.Order.Management.Infrastructure.Diagnostics;
using SADC.Order.Management.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, configuration) => configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddInfrastructure(builder.Configuration);

    // OpenTelemetry — Tracing & Metrics
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService($"{TelemetryConstants.ServiceName}.Worker"))
        .WithTracing(tracing => tracing
            .AddSource(TelemetryConstants.ServiceName)
            .AddConsoleExporter())
        .WithMetrics(metrics => metrics
            .AddMeter(TelemetryConstants.ServiceName)
            .AddConsoleExporter());

    builder.Services.AddHostedService<OrderConsumerWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
