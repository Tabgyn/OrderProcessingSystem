using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using SharedKernel.Models;

namespace OrderService.Consumers;

public class PaymentFailedConsumer : RabbitMqConsumer<PaymentFailed>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "order-service-paymentfailed";
    protected override string[] RoutingKeys => new[] { "event.paymentfailed" };

    public PaymentFailedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<PaymentFailedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(PaymentFailed @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<PaymentFailedConsumer>>();

        var order = await repository.GetByIdAsync(@event.OrderId, cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for payment failure", @event.OrderId);
            return;
        }

        order.Status = OrderStatus.Failed;
        await repository.UpdateAsync(order, cancellationToken);

        // Publish InventoryReleased event to release reserved inventory
        var inventoryReleasedEvent = new InventoryReleased
        {
            OrderId = @event.OrderId,
            ReservationId = Guid.NewGuid(), // This should ideally come from the reservation
            ReleasedAt = DateTime.UtcNow
        };

        await eventBus.PublishAsync(inventoryReleasedEvent, cancellationToken);

        // Publish OrderCancelled event
        var orderCancelledEvent = new OrderCancelled
        {
            OrderId = @event.OrderId,
            Reason = $"Payment failed: {@event.Reason}",
            CancelledAt = DateTime.UtcNow
        };

        await eventBus.PublishAsync(orderCancelledEvent, cancellationToken);

        logger.LogInformation("Order {OrderId} failed due to payment failure: {Reason}",
            @event.OrderId, @event.Reason);
    }
}