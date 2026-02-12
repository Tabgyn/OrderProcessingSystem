using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace OrderService.Infrastructure;

public class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly RabbitMqSettings _settings;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized = false;

    public RabbitMqEventBus(RabbitMqSettings settings, ILogger<RabbitMqEventBus> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized) return;

            var factory = new ConnectionFactory
            {
                HostName = _settings.Host,
                Port = _settings.Port,
                UserName = _settings.Username,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            // Declare exchange for events
            await _channel.ExchangeDeclareAsync(
                exchange: "order-processing-events",
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            _isInitialized = true;
            _logger.LogInformation("RabbitMQ EventBus initialized");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        await EnsureInitializedAsync(cancellationToken);

        try
        {
            var eventType = @event.EventType;
            var routingKey = $"event.{eventType.ToLowerInvariant()}";

            var message = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                Type = eventType,
                MessageId = @event.EventId.ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel!.BasicPublishAsync(
                exchange: "order-processing-events",
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to routing key {RoutingKey}",
                eventType, @event.EventId, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish event {EventType} with ID {EventId}",
                @event.EventType, @event.EventId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _initLock.Dispose();

        if (_channel != null)
        {
            await _channel.CloseAsync();
            await _channel.DisposeAsync();
        }

        if (_connection != null)
        {
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }
}