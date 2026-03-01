using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SADC.Order.Management.Infrastructure.Diagnostics;

/// <summary>
/// Central definitions for OpenTelemetry instrumentation.
/// ActivitySource provides distributed tracing; Meter provides metrics.
/// </summary>
public static class TelemetryConstants
{
    public const string ServiceName = "SADC.Order.Management";

    // ── Tracing ────────────────────────────────────────────────
    public static readonly ActivitySource ActivitySource = new(ServiceName, "1.0.0");

    // ── Metrics ────────────────────────────────────────────────
    public static readonly Meter Meter = new(ServiceName, "1.0.0");

    // Request metrics
    public static readonly Counter<long> RequestCount =
        Meter.CreateCounter<long>("sadc.api.requests.total", "requests", "Total HTTP requests processed");

    // FX cache metrics
    public static readonly Counter<long> FxCacheHits =
        Meter.CreateCounter<long>("sadc.fx.cache.hits", "hits", "FX rate cache hits");

    public static readonly Counter<long> FxCacheMisses =
        Meter.CreateCounter<long>("sadc.fx.cache.misses", "misses", "FX rate cache misses");

    // Order metrics
    public static readonly Counter<long> OrdersCreated =
        Meter.CreateCounter<long>("sadc.orders.created.total", "orders", "Total orders created");

    public static readonly Counter<long> OutboxMessagesPublished =
        Meter.CreateCounter<long>("sadc.outbox.published.total", "messages", "Total outbox messages published");

    public static readonly Histogram<double> FxConversionDuration =
        Meter.CreateHistogram<double>("sadc.fx.conversion.duration", "ms", "FX conversion duration in milliseconds");
}
