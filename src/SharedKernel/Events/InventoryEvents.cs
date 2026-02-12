namespace SharedKernel.Events;

public class InventoryReserved : BaseEvent
{
    public override string EventType => nameof(InventoryReserved);

    public Guid OrderId { get; set; }
    public Guid ReservationId { get; set; }
    public List<ReservedItem> ReservedItems { get; set; } = new();
    public DateTime ReservedAt { get; set; }
}

public class InventoryReservationFailed : BaseEvent
{
    public override string EventType => nameof(InventoryReservationFailed);

    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<Guid> UnavailableProductIds { get; set; } = new();
    public DateTime FailedAt { get; set; }
}

public class InventoryReleased : BaseEvent
{
    public override string EventType => nameof(InventoryReleased);

    public Guid OrderId { get; set; }
    public Guid ReservationId { get; set; }
    public DateTime ReleasedAt { get; set; }
}

public class ReservedItem
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}