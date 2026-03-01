using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SADC.Order.Management.Domain.Enums;
using SADC.Order.Management.Infrastructure.Messaging;
using SADC.Order.Management.Infrastructure.Persistence;

namespace SADC.Order.Management.Worker;

/// <summary>
/// Consumes OrderCreated messages from RabbitMQ.
/// Simulates order allocation and transitions order to Fulfilled status.
/// Implements idempotent processing via message deduplication.
/// </summary>
public class OrderConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<OrderConsumerWorker> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    private const string Exchange = "orders";
    private const string Queue = "order-created-fulfillment";
    private const string RoutingKey = "ordercreated";
    private const string DeadLetterExchange = "orders-dlx";
    private const string DeadLetterQueue = "order-created-fulfillment-dlq";

    public OrderConsumerWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqSettings> settings,
        ILogger<OrderConsumerWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // Dead letter exchange
        await _channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "#", cancellationToken: stoppingToken);

        // Main exchange and queue
        await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(
            queue: Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = RoutingKey
            },
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(Queue, Exchange, RoutingKey, cancellationToken: stoppingToken);

        // Prefetch 1 at a time for reliable processing
        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Received message: {Body}", body);

                var message = JsonSerializer.Deserialize<OutboxMessagePayload>(body);
                if (message?.Payload is not null)
                {
                    var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Payload);
                    if (orderEvent is not null)
                    {
                        await ProcessOrderCreatedAsync(orderEvent, stoppingToken);
                    }
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message. Sending to DLQ.");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Worker started. Consuming from queue: {Queue}", Queue);

        // Keep running until cancelled
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderManagementDbContext>();

        var order = await context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderEvent.OrderId, cancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found. Skipping.", orderEvent.OrderId);
            return;
        }

        // Idempotent: skip if already fulfilled or cancelled
        if (order.Status is OrderStatus.Fulfilled or OrderStatus.Cancelled)
        {
            _logger.LogInformation("Order {OrderId} is already {Status}. Skipping.",
                order.Id, order.Status);
            return;
        }

        // Simulate allocation delay
        await Task.Delay(500, cancellationToken);

        // Transition: Pending → Paid → Fulfilled
        if (order.Status == OrderStatus.Pending)
        {
            order.TryTransitionTo(OrderStatus.Paid);
            _logger.LogInformation("Order {OrderId} transitioned to Paid", order.Id);
        }

        if (order.Status == OrderStatus.Paid)
        {
            order.TryTransitionTo(OrderStatus.Fulfilled);
            _logger.LogInformation("Order {OrderId} transitioned to Fulfilled", order.Id);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            _connection.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    // DTOs for deserializing messages
    private record OutboxMessagePayload(Guid Id, string Type, string Payload, DateTime CreatedAtUtc);
    private record OrderCreatedEvent(Guid OrderId, Guid CustomerId, string CurrencyCode, decimal TotalAmount);
}
