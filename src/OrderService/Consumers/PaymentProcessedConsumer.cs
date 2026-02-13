using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using SharedKernel.Models;

namespace OrderService.Consumers;

public class PaymentProcessedConsumer : RabbitMqConsumer<PaymentProcessed>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "order-service-paymentprocessed";
    protected override string[] RoutingKeys => new[] { "event.paymentprocessed" };

    public PaymentProcessedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<PaymentProcessedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(PaymentProcessed @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentProcessedConsumer>>();

        var order = await repository.GetByIdAsync(@event.OrderId, cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for payment processing", @event.OrderId);
            return;
        }

        order.Status = OrderStatus.Confirmed;
        await repository.UpdateAsync(order, cancellationToken);

        // Publish OrderConfirmed event
        var orderConfirmedEvent = new OrderConfirmed
        {
            OrderId = @event.OrderId,
            ConfirmedAt = DateTime.UtcNow
        };

        await eventBus.PublishAsync(orderConfirmedEvent, cancellationToken);

        logger.LogInformation("Order {OrderId} confirmed after successful payment", @event.OrderId);
    }
}