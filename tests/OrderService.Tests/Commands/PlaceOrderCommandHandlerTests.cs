using Moq;
using OrderService.Application.Commands;
using OrderService.Domain;
using OrderService.Infrastructure;
using SharedKernel.Events;
using SharedKernel.Messaging;
using Microsoft.Extensions.Logging;

namespace OrderService.Tests.Commands;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IEventStoreRepository> _eventStoreRepositoryMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<PlaceOrderCommandHandler>> _loggerMock;
    private readonly PlaceOrderCommandHandler _handler;

    public PlaceOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _eventStoreRepositoryMock = new Mock<IEventStoreRepository>();
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<PlaceOrderCommandHandler>>();

        _handler = new PlaceOrderCommandHandler(
            _orderRepositoryMock.Object,
            _eventStoreRepositoryMock.Object,
            _eventBusMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ValidOrder_ReturnsSuccess()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Laptop", 2, 999.99m)
            }
        );

        _orderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStoreRepositoryMock
            .Setup(r => r.SaveEventAsync(It.IsAny<Guid>(), It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1999.98m, result.Value.TotalAmount);
        Assert.NotEqual(Guid.Empty, result.Value.OrderId);
    }

    [Fact]
    public async Task HandleAsync_EmptyItems_ReturnsFailure()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>()
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Order must contain at least one item", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ZeroQuantity_ReturnsFailure()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Laptop", 0, 999.99m)
            }
        );

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("All items must have quantity greater than 0", result.Error);
    }

    [Fact]
    public async Task HandleAsync_ValidOrder_PublishesOrderPlacedEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var command = new PlaceOrderCommand(
            CustomerId: customerId,
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Mouse", 1, 29.99m)
            }
        );

        _orderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStoreRepositoryMock
            .Setup(r => r.SaveEventAsync(It.IsAny<Guid>(), It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _eventBusMock.Verify(
            b => b.PublishAsync(
                It.Is<OrderPlaced>(e => e.CustomerId == customerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidOrder_SavesEventToStore()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Keyboard", 1, 79.99m)
            }
        );

        _orderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStoreRepositoryMock
            .Setup(r => r.SaveEventAsync(It.IsAny<Guid>(), It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.HandleAsync(command);

        // Assert
        _eventStoreRepositoryMock.Verify(
            r => r.SaveEventAsync(
                It.IsAny<Guid>(),
                It.IsAny<OrderPlaced>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MultipleItems_CalculatesTotalAmountCorrectly()
    {
        // Arrange
        var command = new PlaceOrderCommand(
            CustomerId: Guid.NewGuid(),
            Items: new List<PlaceOrderItem>
            {
                new(Guid.NewGuid(), "Laptop", 2, 999.99m),    // 1999.98
                new(Guid.NewGuid(), "Mouse", 3, 29.99m),       // 89.97
                new(Guid.NewGuid(), "Keyboard", 1, 79.99m)     // 79.99
            }
        );

        _orderRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventStoreRepositoryMock
            .Setup(r => r.SaveEventAsync(It.IsAny<Guid>(), It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<OrderPlaced>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2169.94m, result.Value.TotalAmount);
    }
}