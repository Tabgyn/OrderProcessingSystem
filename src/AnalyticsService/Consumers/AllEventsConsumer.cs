using System.Text;
using System.Text.Json;
using AnalyticsService.Domain;
using AnalyticsService.Infrastructure;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace AnalyticsService.Consumers;

public class AllEventsConsumer : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AllEventsConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public AllEventsConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<AllEventsConsumer> logger)
    {
        _settings = settings;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            ConsumerDispatchConcurrency = 1
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "order-processing-events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: "analytics-service-allevents",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Bind to all event types
        await _channel.QueueBindAsync(
            queue: "analytics-service-allevents",
            exchange: "order-processing-events",
            routingKey: "event.*",
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var eventType = ea.BasicProperties.Type ?? "Unknown";

                await HandleEventAsync(eventType, message, stoppingToken);
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event");
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(queue: "analytics-service-allevents", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Analytics consumer started - listening to all events");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleEventAsync(string eventType, string eventData, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnalyticsRepository>();
        var metricsService = scope.ServiceProvider.GetRequiredService<IMetricsService>();

        // Log all events
        var eventLog = new EventLog
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            EventData = eventData,
            OccurredAt = DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow
        };

        await repository.LogEventAsync(eventLog, cancellationToken);

        // Update metrics based on event type
        switch (eventType)
        {
            case "OrderPlaced":
                var orderPlaced = JsonSerializer.Deserialize<OrderPlaced>(eventData);
                if (orderPlaced != null)
                {
                    await metricsService.IncrementOrderPlacedAsync(orderPlaced.TotalAmount, cancellationToken);
                }
                break;

            case "OrderConfirmed":
                await metricsService.IncrementOrderConfirmedAsync(cancellationToken);
                break;

            case "OrderCancelled":
                await metricsService.IncrementOrderCancelledAsync(cancellationToken);
                break;

            case "PaymentFailed":
            case "InventoryReservationFailed":
                await metricsService.IncrementOrderFailedAsync(cancellationToken);
                break;
        }

        _logger.LogInformation("📊 Event logged: {EventType}", eventType);
    }

    public override void Dispose()
    {
        try
        {
            if (_channel != null)
            {
                _channel.CloseAsync().GetAwaiter().GetResult();
                _channel.Dispose();
            }

            if (_connection != null)
            {
                _connection.CloseAsync().GetAwaiter().GetResult();
                _connection.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing analytics consumer");
        }
        finally
        {
            base.Dispose();
        }
    }
}