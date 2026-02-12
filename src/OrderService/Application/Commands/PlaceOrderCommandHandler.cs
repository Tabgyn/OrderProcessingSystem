using OrderService.Domain;
using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using SharedKernel.Models;

namespace OrderService.Application.Commands;

public interface IPlaceOrderCommandHandler
{
    Task<Result<PlaceOrderResult>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default);
}

public class PlaceOrderCommandHandler : IPlaceOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventStoreRepository _eventStoreRepository;
    private readonly IEventBus _eventBus;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IEventStoreRepository eventStoreRepository,
        IEventBus eventBus,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _eventStoreRepository = eventStoreRepository;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Result<PlaceOrderResult>> HandleAsync(PlaceOrderCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validation
            if (!command.Items.Any())
                return Result.Failure<PlaceOrderResult>("Order must contain at least one item");

            if (command.Items.Any(i => i.Quantity <= 0))
                return Result.Failure<PlaceOrderResult>("All items must have quantity greater than 0");

            // Create order
            var orderId = Guid.NewGuid();
            var orderItems = command.Items.Select(i => new Domain.OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.Quantity * i.UnitPrice
            }).ToList();

            var totalAmount = orderItems.Sum(i => i.TotalPrice);

            var order = new Order
            {
                Id = orderId,
                CustomerId = command.CustomerId,
                TotalAmount = totalAmount,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = orderItems
            };

            // Save to read model
            await _orderRepository.AddAsync(order, cancellationToken);

            // Create and save event
            var orderPlacedEvent = new OrderPlaced
            {
                OrderId = orderId,
                CustomerId = command.CustomerId,
                Items = command.Items.Select(i => new SharedKernel.Events.OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                TotalAmount = totalAmount,
                PlacedAt = DateTime.UtcNow
            };

            await _eventStoreRepository.SaveEventAsync(orderId, orderPlacedEvent, cancellationToken);

            // Publish event to message bus
            await _eventBus.PublishAsync(orderPlacedEvent, cancellationToken);

            _logger.LogInformation("Order {OrderId} placed successfully for customer {CustomerId}",
                orderId, command.CustomerId);

            return Result.Success(new PlaceOrderResult(orderId, totalAmount, DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order for customer {CustomerId}", command.CustomerId);
            return Result.Failure<PlaceOrderResult>($"Failed to place order: {ex.Message}");
        }
    }
}