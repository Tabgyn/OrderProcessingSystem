namespace OrderService.Application.Commands;

public record PlaceOrderCommand(
    Guid CustomerId,
    List<PlaceOrderItem> Items
);

public record PlaceOrderItem(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record PlaceOrderResult(
    Guid OrderId,
    decimal TotalAmount,
    DateTime PlacedAt
);