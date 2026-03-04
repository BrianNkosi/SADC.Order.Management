using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Domain.Entities;
using SADC.Order.Management.Infrastructure.Persistence;

namespace SADC.Order.Management.Infrastructure.Messaging;

/// <summary>
/// Background service that polls the outbox table and publishes pending messages to RabbitMQ.
/// Implements retry with exponential backoff.
/// </summary>
public class OutboxPublisherService(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    ILogger<OutboxPublisherService> logger) : BackgroundService
{
    private const string Exchange = "orders";
    private const int PollingIntervalMs = 2000;
    private const int MaxRetries = 5;
    private const int BaseBackoffSeconds = 2;
    private const int BatchSize = 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher loop");
            }

            await Task.Delay(PollingIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        var candidates = await FetchCandidatesAsync(context, cancellationToken);
        if (candidates.Count == 0)
            return;

        var eligible = candidates.Where(IsReadyForProcessing).ToList();

        foreach (var message in eligible)
            await TryPublishAsync(message, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Task<List<OutboxMessage>> FetchCandidatesAsync(
        OrderManagementDbContext context, CancellationToken cancellationToken) =>
        context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

    private static bool IsReadyForProcessing(OutboxMessage message)
    {
        if (message.RetryCount == 0)
            return true;

        var backoff = TimeSpan.FromSeconds(BaseBackoffSeconds * Math.Pow(2, message.RetryCount - 1));
        return DateTime.UtcNow >= message.CreatedAtUtc + backoff;
    }

    private async Task TryPublishAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(
                Exchange,
                message.Type.ToLowerInvariant(),
                new
                {
                    message.Id,
                    message.AggregateType,
                    message.AggregateId,
                    message.Type,
                    message.Payload,
                    message.Version,
                    message.OccurredAtUtc,
                    message.CreatedAtUtc
                },
                cancellationToken);

            message.ProcessedAtUtc = DateTime.UtcNow;
            message.Error = null;

            logger.LogInformation("Published outbox message {MessageId} ({Type})", message.Id, message.Type);
        }
        catch (Exception ex)
        {
            message.RetryCount++;
            message.Error = ex.Message;

            var nextBackoff = TimeSpan.FromSeconds(BaseBackoffSeconds * Math.Pow(2, message.RetryCount - 1));
            logger.LogWarning(ex,
                "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxRetries}). Next retry in {BackoffSeconds}s",
                message.Id, message.RetryCount, MaxRetries, nextBackoff.TotalSeconds);
        }
    }
}
