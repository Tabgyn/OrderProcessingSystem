using InventoryService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace InventoryService.Consumers;

public class InventoryReleasedConsumer : RabbitMqConsumer<InventoryReleased>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "inventory-service-inventoryreleased";
    protected override string[] RoutingKeys => new[] { "event.inventoryreleased" };

    public InventoryReleasedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<InventoryReleasedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(InventoryReleased @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<InventoryReleasedConsumer>>();

        await repository.ReleaseReservationAsync(@event.OrderId, cancellationToken);

        logger.LogInformation("Inventory released for order {OrderId}", @event.OrderId);
    }
}