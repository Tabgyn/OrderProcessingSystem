namespace SharedKernel.Events;

public class OrderPlaced : BaseEvent
{
    public override string EventType => nameof(OrderPlaced);

    public Guid OrderId { get; set; }
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public DateTime PlacedAt { get; set; }
}

public class OrderConfirmed : BaseEvent
{
    public override string EventType => nameof(OrderConfirmed);

    public Guid OrderId { get; set; }
    public DateTime ConfirmedAt { get; set; }
}

public class OrderCancelled : BaseEvent
{
    public override string EventType => nameof(OrderCancelled);

    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}