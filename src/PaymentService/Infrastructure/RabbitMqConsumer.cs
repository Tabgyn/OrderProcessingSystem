using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace PaymentService.Infrastructure;

public abstract class RabbitMqConsumer<TEvent> : BackgroundService where TEvent : class, IEvent
{
    private readonly RabbitMqSettings _settings;
    private readonly ILogger _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    protected abstract string QueueName { get; }
    protected abstract string[] RoutingKeys { get; }

    protected RabbitMqConsumer(RabbitMqSettings settings, ILogger logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // Wait for RabbitMQ to be ready

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

        // Declare exchange
        await _channel.ExchangeDeclareAsync(
            exchange: "order-processing-events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Declare queue
        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        // Bind queue to routing keys
        foreach (var routingKey in RoutingKeys)
        {
            await _channel.QueueBindAsync(
                queue: QueueName,
                exchange: "order-processing-events",
                routingKey: routingKey,
                arguments: null,
                cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var @event = JsonSerializer.Deserialize<TEvent>(message);

                if (@event != null)
                {
                    await HandleEventAsync(@event, stoppingToken);
                    await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "Processed event {EventType} from queue {QueueName}",
                        @event.EventType, QueueName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message from queue {QueueName}", QueueName);

                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(queue: QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation(
            "Consumer started for queue {QueueName} listening to routing keys: {RoutingKeys}",
            QueueName, string.Join(", ", RoutingKeys));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    protected abstract Task HandleEventAsync(TEvent @event, CancellationToken cancellationToken);

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
            _logger.LogError(ex, "Error disposing RabbitMQ consumer");
        }
        finally
        {
            base.Dispose();
        }
    }
}