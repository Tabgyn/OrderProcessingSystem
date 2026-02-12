namespace SharedKernel.Models;

public enum OrderStatus
{
    Pending = 0,
    InventoryReserved = 1,
    PaymentProcessed = 2,
    Confirmed = 3,
    Cancelled = 4,
    Failed = 5
}