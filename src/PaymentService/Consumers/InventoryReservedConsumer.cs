using PaymentService.Domain;
using PaymentService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;

namespace PaymentService.Consumers;

public class InventoryReservedConsumer : RabbitMqConsumer<InventoryReserved>
{
    private readonly IServiceProvider _serviceProvider;

    protected override string QueueName => "payment-service-inventoryreserved";
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
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        // Create payment record
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = @event.OrderId,
            Amount = 0, // We don't have amount in InventoryReserved event, would need to get from order
            PaymentMethod = "CreditCard",
            Status = PaymentStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        await repository.CreatePaymentAsync(payment, cancellationToken);

        // Process payment through gateway
        var result = await paymentGateway.ProcessPaymentAsync(payment.Amount, payment.PaymentMethod, cancellationToken);

        if (result.IsSuccess)
        {
            payment.Status = PaymentStatus.Completed;
            payment.TransactionId = result.TransactionId;
            payment.ProcessedAt = DateTime.UtcNow;

            await repository.UpdatePaymentAsync(payment, cancellationToken);

            var paymentProcessedEvent = new PaymentProcessed
            {
                OrderId = @event.OrderId,
                PaymentId = payment.Id,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                TransactionId = result.TransactionId!,
                ProcessedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(paymentProcessedEvent, cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.ErrorCode = result.ErrorCode;
            payment.ErrorMessage = result.ErrorMessage;

            await repository.UpdatePaymentAsync(payment, cancellationToken);

            var paymentFailedEvent = new PaymentFailed
            {
                OrderId = @event.OrderId,
                Amount = payment.Amount,
                Reason = result.ErrorMessage ?? "Payment processing failed",
                ErrorCode = result.ErrorCode ?? "UNKNOWN",
                FailedAt = DateTime.UtcNow
            };

            await eventBus.PublishAsync(paymentFailedEvent, cancellationToken);
        }
    }
}