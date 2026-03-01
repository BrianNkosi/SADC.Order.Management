using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SADC.Order.Management.Application.Common.Interfaces;

namespace SADC.Order.Management.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMQ";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
}

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RabbitMqPublisher(IOptions<RabbitMqSettings> settings, ILogger<RabbitMqPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message, CancellationToken cancellationToken = default) where T : class
    {
        var channel = await GetChannelAsync(cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = Guid.NewGuid().ToString()
        };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Published message to {Exchange}/{RoutingKey}: {MessageId}",
            exchange, routingKey, properties.MessageId);
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is null or { IsOpen: false })
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.HostName,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
            }

            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(
                    publisherConfirmationsEnabled: true,
                    publisherConfirmationTrackingEnabled: false),
                cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync();
            _channel.Dispose();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _semaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
