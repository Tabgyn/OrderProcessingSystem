using InventoryService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace InventoryService.Consumers;

public class OrderPlacedConsumer : RabbitMqConsumer<OrderPlaced>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "inventory-service-orderplaced";
    protected override string[] RoutingKeys => new[] { "event.orderplaced" };

    public OrderPlacedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<OrderPlacedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(OrderPlaced @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var items = @event.Items.Select(i => (i.ProductId, i.Quantity)).ToList();
        var reserved = await repository.ReserveInventoryAsync(@event.OrderId, items, cancellationToken);

        if (reserved)
        {
            var inventoryReservedEvent = new InventoryReserved
            {
                OrderId = @event.OrderId,
                ReservationId = Guid.NewGuid(),
                ReservedItems = @event.Items.Select(i => new ReservedItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity
                }).ToList(),
                ReservedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(inventoryReservedEvent, cancellationToken);
        }
        else
        {
            var inventoryFailedEvent = new InventoryReservationFailed
            {
                OrderId = @event.OrderId,
                Reason = "Insufficient inventory",
                UnavailableProductIds = @event.Items.Select(i => i.ProductId).ToList(),
                FailedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(inventoryFailedEvent, cancellationToken);
        }
    }
}