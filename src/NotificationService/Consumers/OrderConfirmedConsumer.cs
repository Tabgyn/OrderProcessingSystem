using NotificationService.Domain;
using NotificationService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace NotificationService.Consumers;

public class OrderConfirmedConsumer : RabbitMqConsumer<OrderConfirmed>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "notification-service-orderconfirmed";
    protected override string[] RoutingKeys => new[] { "event.orderconfirmed" };

    public OrderConfirmedConsumer(
        RabbitMqSettings settings,
        IServiceProvider serviceProvider,
        ILogger<OrderConfirmedConsumer> logger) : base(settings, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task HandleEventAsync(OrderConfirmed @event, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        // We need CustomerId - in real system would query OrderService or include in event
        var customerId = Guid.NewGuid(); // Placeholder

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            CustomerId = customerId,
            Type = NotificationType.OrderConfirmed,
            Subject = "Order Confirmed",
            Message = $"Great news! Your order #{@event.OrderId} has been confirmed and is being processed.",
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