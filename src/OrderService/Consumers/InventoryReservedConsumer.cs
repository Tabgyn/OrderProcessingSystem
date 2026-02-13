using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using SharedKernel.Models;

namespace OrderService.Consumers;

public class InventoryReservedConsumer : RabbitMqConsumer<InventoryReserved>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "order-service-inventoryreserved";
    protected override string[] RoutingKeys => new[] { "event.inventoryreserved" };

    public InventoryReservedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<InventoryReservedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(InventoryReserved @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryReservedConsumer>>();

        var order = await repository.GetByIdAsync(@event.OrderId, cancellationToken);

        if (order == null)
        {
            logger.LogWarning("Order {OrderId} not found for inventory reservation", @event.OrderId);
            return;
        }

        order.Status = OrderStatus.InventoryReserved;
        await repository.UpdateAsync(order, cancellationToken);

        logger.LogInformation("Order {OrderId} status updated to InventoryReserved", @event.OrderId);
    }
}