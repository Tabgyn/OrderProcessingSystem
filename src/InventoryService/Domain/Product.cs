namespace InventoryService.Domain;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InventoryReservation
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public bool IsActive { get; set; }

    public List<ReservationItem> Items { get; set; } = new();
}

public class ReservationItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}