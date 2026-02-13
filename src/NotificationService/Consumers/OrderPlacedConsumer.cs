using NotificationService.Domain;
using NotificationService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace NotificationService.Consumers;

public class OrderPlacedConsumer : RabbitMqConsumer<OrderPlaced>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "notification-service-orderplaced";
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
        var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            Type = NotificationType.OrderPlaced,
            Subject = "Order Placed Successfully",
            Message = $"Your order #{@event.OrderId} has been placed successfully. Total amount: ${@event.TotalAmount:F2}",
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