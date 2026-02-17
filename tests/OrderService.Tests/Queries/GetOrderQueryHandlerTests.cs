using Moq;
using OrderService.Application.Queries;
using OrderService.Domain;
using OrderService.Infrastructure;
using SharedKernel.Models;

namespace OrderService.Tests.Queries;

public class GetOrderQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderQueryHandler _handler;

    public GetOrderQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrderQueryHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task HandleAsync_ExistingOrder_ReturnsSuccess()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            CustomerId = Guid.NewGuid(),
            TotalAmount = 999.99m,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.HandleAsync(new GetOrderQuery(orderId));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(orderId, result.Value.Id);
        Assert.Equal(OrderStatus.Confirmed, result.Value.Status);
    }

    [Fact]
    public async Task HandleAsync_NonExistingOrder_ReturnsFailure()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _handler.HandleAsync(new GetOrderQuery(orderId));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains(orderId.ToString(), result.Error);
    }
}