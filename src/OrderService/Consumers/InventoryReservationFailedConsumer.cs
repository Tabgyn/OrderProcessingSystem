using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using SharedKernel.Models;

namespace OrderService.Consumers;

public class InventoryReservationFailedConsumer : RabbitMqConsumer<InventoryReservationFailed>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "order-service-inventoryreservationfailed";
    protected override string[] RoutingKeys => new[] { "event.inventoryreservationfailed" };

    public InventoryReservationFailedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<InventoryReservationFailedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(InventoryReservationFailed @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryReservationFailedConsumer>>();

        var order = await repository.GetByIdAsync(@event.OrderId, cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for inventory reservation failure", @event.OrderId);
            return;
        }

        order.Status = OrderStatus.Cancelled;
        await repository.UpdateAsync(order, cancellationToken);

        // Publish OrderCancelled event
        var orderCancelledEvent = new OrderCancelled
        {
            OrderId = @event.OrderId,
            Reason = @event.Reason,
            CancelledAt = DateTime.UtcNow
        };

        await eventBus.PublishAsync(orderCancelledEvent, cancellationToken);

        logger.LogInformation("Order {OrderId} cancelled due to inventory reservation failure: {Reason}",
            @event.OrderId, @event.Reason);
    }
}