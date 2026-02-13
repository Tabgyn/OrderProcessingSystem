using NotificationService.Domain;
using NotificationService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace NotificationService.Consumers;

public class OrderCancelledConsumer : RabbitMqConsumer<OrderCancelled>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "notification-service-ordercancelled";
    protected override string[] RoutingKeys => new[] { "event.ordercancelled" };

    public OrderCancelledConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<OrderCancelledConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(OrderCancelled @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        // We need CustomerId - placeholder
        var customerId = Guid.NewGuid();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            CustomerId = customerId,
            Type = NotificationType.OrderCancelled,
            Subject = "Order Cancelled",
            Message = $"Your order #{@event.OrderId} has been cancelled. Reason: {@event.Reason}",
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreateAsync(notification, cancellationToken);

        var sent = await sender.SendAsync(notification, cancellationToken);

        if (sent)
        {
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            await repository.UpdateAsync(notification, cancellationToken);
        }
    }
}