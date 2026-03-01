using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SADC.Order.Management.Application.Common.Interfaces;
using SADC.Order.Management.Infrastructure.Persistence;

namespace SADC.Order.Management.Infrastructure.Messaging;

/// <summary>
/// Background service that polls the outbox table and publishes pending messages to RabbitMQ.
/// Implements retry with exponential backoff.
/// </summary>
public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<OutboxPublisherService> _logger;

    private const string Exchange = "orders";
    private const int PollingIntervalMs = 2000;
    private const int MaxRetries = 5;
    private const int BaseBackoffSeconds = 2;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IMessagePublisher publisher,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(PollingIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        var now = DateTime.UtcNow;

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Apply exponential backoff: skip messages whose backoff period hasn't elapsed
        var eligibleMessages = messages.Where(m =>
        {
            if (m.RetryCount == 0) return true;
            var backoffDelay = TimeSpan.FromSeconds(BaseBackoffSeconds * Math.Pow(2, m.RetryCount - 1));
            return now >= m.CreatedAtUtc.Add(backoffDelay * m.RetryCount);
        }).ToList();

        foreach (var message in eligibleMessages)
        {
            try
            {
                var routingKey = message.Type.ToLowerInvariant();

                await _publisher.PublishAsync(
                    Exchange,
                    routingKey,
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

                _logger.LogInformation("Published outbox message {MessageId} ({Type})", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                var nextBackoff = TimeSpan.FromSeconds(BaseBackoffSeconds * Math.Pow(2, message.RetryCount - 1));
                _logger.LogWarning(ex,
                    "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxRetries}). " +
                    "Next retry in {BackoffSeconds}s",
                    message.Id, message.RetryCount, MaxRetries, nextBackoff.TotalSeconds);
            }
        }

        if (messages.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
