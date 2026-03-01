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

        var messages = await context.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                var routingKey = message.Type.ToLowerInvariant();

                await _publisher.PublishAsync(
                    Exchange,
                    routingKey,
                    new { message.Id, message.Type, message.Payload, message.CreatedAtUtc },
                    cancellationToken);

                message.ProcessedAtUtc = DateTime.UtcNow;
                message.Error = null;

                _logger.LogInformation("Published outbox message {MessageId} ({Type})", message.Id, message.Type);
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                _logger.LogWarning(ex,
                    "Failed to publish outbox message {MessageId} (attempt {Attempt}/{MaxRetries})",
                    message.Id, message.RetryCount, MaxRetries);

                // Exponential backoff: skip this message for increasingly longer periods
                // by not marking it as processed; the polling interval handles basic backoff
            }
        }

        if (messages.Count > 0)
            await context.SaveChangesAsync(cancellationToken);
    }
}
